using System.Collections.Immutable;
using Grpc.Core;
using KL.Calc.Controller;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Grpc;
using KL.Common.Models;
using KL.Common.Utils;
using ILogger = Serilog.ILogger;

namespace KL.Calc.Computer;


public static class CalcRequestHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(CalcRequestHandler));

    private static async Task CalcMomentum(string symbol) {
        await RedisMomentumController.Set(symbol, await MomentumComputer.CalcMomentum(symbol));
    }

    private static async Task CalcSrLevel(IEnumerable<string> symbols) {
        await SrLevelController.UpdateAll(SrLevelComputer.CalcLevels(symbols));
    }

    private static IEnumerable<Task> CalcGlobal(string symbol) {
        return new[] { CalcMomentum(symbol) };
    }

    private static IEnumerable<Task> CalcGlobal(IList<string> symbols) {
        return symbols.Select(CalcMomentum).Concat(new[] { CalcSrLevel(symbols) });
    }

    private static async Task CalcAll(
        (string Symbol, int Period) c,
        IImmutableDictionary<string, ImmutableDictionary<int, IEnumerable<GroupedHistoryDataModel>>> groupedDict,
        MongoSession session
    ) {
        var grouped = groupedDict[c.Symbol][c.Period].ToImmutableList();

        if (grouped.IsEmpty) {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"{c.Symbol} @ {c.Period} does not have grouped history data available"
                )
            );
        }

        var calculated = (await HistoryDataComputer.CalcAll(grouped, c.Period)).ToImmutableArray();
        await CalculatedDataController.AddData(session, calculated);
    }

    public static async Task CalcAll(IList<string> symbols, CancellationToken cancellationToken) {
        var periodMins = PxConfigController.Config.Periods.Select(r => r.PeriodMin).ToImmutableArray();
        var groupedDict = await HistoryDataGrouper.GetGroupedDictOfAll(
            symbols,
            periodMins
        );

        var combinations = symbols
            .SelectMany(symbol => periodMins.Select(period => (Symbol: symbol, Period: period)))
            .ToImmutableArray();

        using var session = await MongoSession.Create();

        await CalculatedDataController.RemoveData(session, combinations);
        // ReSharper disable once AccessToDisposedClosure
        await Task.WhenAll(
            combinations
                .Select(r => CalcAll(r, groupedDict, session))
                .Concat(CalcGlobal(symbols))
        );

        await session.Session.CommitTransactionAsync(cancellationToken);

        GrpcSystemEventCaller.OnCalculatedAsync(symbols, cancellationToken);

        Log.Information(
            "Completed indicator calculation of calc all request of {@Symbols} x {@Periods}",
            symbols,
            periodMins
        );
    }

    private static Task<
        IImmutableDictionary<string, ImmutableDictionary<int, IEnumerable<GroupedHistoryDataModel>>>
    > CalcPartialGetHistory(IList<string> symbols, IList<int> periodMins, int limit) {
        return HistoryDataGrouper.GetGroupedDictOfLastN(
            symbols,
            periodMins,
            limit
        );
    }

    private static async Task<
        Dictionary<(string Symbol, int Period), IEnumerable<CalculatedDataModel>>
    > CalcPartialGetCalculated(ImmutableArray<(string Symbol, int Period)> combinations, int limit) {
        var groupedCalculatedTasks = combinations
            .Select(
                r => Task.Run(
                    () => (
                        r.Symbol,
                        r.Period,
                        CalculatedData: CalculatedDataController.GetData(r.Symbol, r.Period, limit)
                    )
                )
            );
        return (await Task.WhenAll(groupedCalculatedTasks).ConfigureAwait(false))
            .ToDictionary(
                r => (r.Symbol, r.Period),
                r => r.CalculatedData
            );
    }

    private static async Task CalcPartial(
        (string Symbol, int Period) c,
        IImmutableDictionary<string, ImmutableDictionary<int, IEnumerable<GroupedHistoryDataModel>>> groupedHistory,
        IReadOnlyDictionary<(string Symbol, int Period), IEnumerable<CalculatedDataModel>> groupedCalculated
    ) {
        try {
            var history = groupedHistory[c.Symbol][c.Period];
            var cachedCalculated = groupedCalculated[c];

            var calculated = (await HistoryDataComputer.CalcPartial(history, cachedCalculated, c.Period))
                .ToImmutableArray();
            await CalculatedDataController.UpdateByEpoch(calculated);
        } catch (InvalidOperationException e) {
            Log.Error(e, "Error when attempting partial calculation for {Symbol} @ {Period}", c.Symbol, c.Period);
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"Unable to perform partial calculation for {c.Symbol} @ {c.Period}, symbol might be unavailable"
                )
            );
        }
    }

    public static async Task CalcPartial(IList<string> symbols, int limit, CancellationToken cancellationToken) {
        // BUG: Calc Partial using data created after bar creation?
        var periodMins = PxConfigController.Config.Periods.Select(r => r.PeriodMin).ToImmutableArray();
        var combinations = symbols
            .SelectMany(symbol => periodMins.Select(period => (Symbol: symbol, Period: period)))
            .ToImmutableArray();

        var historyDataTask = CalcPartialGetHistory(symbols, periodMins, limit);
        var calculatedDataTask = CalcPartialGetCalculated(combinations, limit);

        await Task.WhenAll(historyDataTask, calculatedDataTask);

        var groupedHistory = await historyDataTask;
        var groupedCalculated = await calculatedDataTask;

        await Task.WhenAll(
            combinations
                .Select(r => CalcPartial(r, groupedHistory, groupedCalculated))
                .Concat(CalcGlobal(symbols))
        );

        GrpcSystemEventCaller.OnCalculatedAsync(symbols, cancellationToken);

        Log.Information(
            "Completed calc partial request {@Symbols} x {@Periods}",
            symbols,
            PxConfigController.Config.Periods.Select(r => r.PeriodMin)
        );
    }

    private static async Task CalcLast(string symbol, int periodMin, decimal lastPx) {
        var data = CalculatedDataController.GetData(symbol, periodMin, 2).ToImmutableArray();

        if (data.IsEmpty) {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"{symbol} @ {periodMin} does not have calculated data available"
                )
            );
        }

        data[^1].Close = lastPx;

        var calculated = HistoryDataComputer.CalcLast(data[^1], data[^2]);
        await CalculatedDataController.UpdateByEpoch(ImmutableArray.Create(new[] { calculated }));
    }

    public static async Task CalcLast(string symbol, CancellationToken cancellationToken) {
        var lastPx = HistoryDataController.GetLastN(symbol, HistoryInterval.Minute, 1).First().Close;

        await Task.WhenAll(
            PxConfigController.Config.Periods
                .Select(r => CalcLast(symbol, r.PeriodMin, lastPx))
                .Concat(CalcGlobal(symbol))
        );
        GrpcSystemEventCaller.OnCalculatedAsync(symbol, cancellationToken);

        Log.Information(
            "Completed calc last request of {Symbol} @ {@Periods}",
            symbol,
            PxConfigController.Config.Periods.Select(r => r.PeriodMin)
        );
    }
}