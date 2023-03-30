using Grpc.Core;
using KL.Calc.Controller;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Grpc;
using KL.Common.Models;
using KL.Proto;
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
        IDictionary<string, IDictionary<int, IEnumerable<GroupedHistoryDataModel>>> groupedDict
    ) {
        var grouped = groupedDict[c.Symbol][c.Period].ToArray();

        if (grouped.IsEmpty()) {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"{c.Symbol} @ {c.Period} does not have grouped history data available"
                )
            );
        }

        Log.Information("Calculating data of {Symbol} @ {Period} from `CalcAll`", c.Symbol, c.Period);
        await CalculatedDataController.AddData(await HistoryDataComputer.CalcAll(grouped, c.Period));
    }

    public static async Task CalcAll(
        IList<string> symbols,
        IServerStreamWriter<PxCalcReply> responseStream,
        CancellationToken cancellationToken
    ) {
        await responseStream.WriteAsync(
            new PxCalcReply { Message = $"Requesting history data of {symbols} for calculation" },
            cancellationToken
        );
        var periodMins = PxConfigController.Config.Periods.Select(r => r.PeriodMin).ToArray();
        var groupedDict = await HistoryDataGrouper.GetGroupedDictOfAll(
            symbols,
            periodMins
        );

        var combinations = symbols
            .SelectMany(symbol => periodMins.Select(period => (Symbol: symbol, Period: period)))
            .ToArray();

        await responseStream.WriteAsync(
            new PxCalcReply { Message = $"Removing history data of {symbols}" },
            cancellationToken
        );
        await CalculatedDataController.RemoveData(combinations);

        // Calculating data one-by-one because it stores the data after calculation,
        // and simultaneous data storing causes Mongo write conflict
        foreach (var combination in combinations) {
            if (cancellationToken.IsCancellationRequested) {
                Log.Warning(
                    "Cancelling `CalcAll` operation for {Symbol} @ {Period}",
                    combination.Symbol,
                    combination.Period
                );
                return;
            }

            await responseStream.WriteAsync(
                new PxCalcReply {
                    Message = $"Calculating history data of {combination.Symbol} @ {combination.Period}"
                },
                cancellationToken
            );
            await CalcAll(combination, groupedDict);
        }

        await responseStream.WriteAsync(
            new PxCalcReply {
                Message = $"Performing global calculation tasks of {symbols}"
            },
            cancellationToken
        );
        await Task.WhenAll(CalcGlobal(symbols));

        GrpcSystemEventCaller.OnCalculatedAsync(symbols, cancellationToken);

        await responseStream.WriteAsync(new PxCalcReply { Message = "Done calculating all data" }, cancellationToken);
        Log.Information(
            "Completed indicator calculation of calc all request of {@Symbols} x {@Periods}",
            symbols,
            periodMins
        );
    }

    private static async Task<
        Dictionary<(string Symbol, int Period), IEnumerable<CalculatedDataModel>>
    > CalcPartialGetCalculated(IEnumerable<(string Symbol, int Period)> combinations, int limit) {
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
        IDictionary<string, IDictionary<int, IEnumerable<GroupedHistoryDataModel>>> groupedHistory,
        IReadOnlyDictionary<(string Symbol, int Period), IEnumerable<CalculatedDataModel>> groupedCalculated
    ) {
        try {
            var history = groupedHistory[c.Symbol][c.Period];
            var cachedCalculated = groupedCalculated[c];

            var calculated = (await HistoryDataComputer.CalcPartial(history, cachedCalculated, c.Period))
                .ToArray();
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
        var periodMins = PxConfigController.Config.Periods.Select(r => r.PeriodMin).ToArray();
        var combinations = symbols
            .Where(PxConfigController.IsMarketOpened)
            .SelectMany(symbol => periodMins.Select(period => (Symbol: symbol, Period: period)))
            .ToArray();

        var historyDataTask = HistoryDataGrouper.GetGroupedDictOfLastN(symbols, periodMins, limit);
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
        var data = CalculatedDataController.GetData(symbol, periodMin, 2).ToArray();

        if (data.IsEmpty()) {
            throw new RpcException(
                new Status(
                    StatusCode.NotFound,
                    $"{symbol} @ {periodMin} does not have calculated data available"
                )
            );
        }

        data[^1].Close = lastPx;

        var calculated = HistoryDataComputer.CalcLast(data[^1], data[^2]);
        await CalculatedDataController.UpdateByEpoch(calculated);
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