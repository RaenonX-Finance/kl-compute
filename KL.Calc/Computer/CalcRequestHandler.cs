using System.Collections.Immutable;
using Grpc.Core;
using KL.Calc.Controller;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Models;
using KL.Common.Utils;
using ILogger = Serilog.ILogger;

namespace KL.Calc.Computer;


public static class CalcRequestHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(CalcRequestHandler));

    public static async Task CalcAll(IList<string> symbols) {
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
        await Task.WhenAll(
            combinations.Select(
                async r => {
                    var grouped = groupedDict[r.Symbol][r.Period].ToImmutableList();

                    if (grouped.IsEmpty) {
                        throw new RpcException(
                            new Status(
                                StatusCode.NotFound,
                                $"{r.Symbol} @ {r.Period} does not have grouped history data available"
                            )
                        );
                    }

                    var calculated
                        = (await HistoryDataComputer.CalcAll(grouped, r.Period))
                        .ToImmutableArray();
                    // ReSharper disable once AccessToDisposedClosure
                    await CalculatedDataController.AddData(session, calculated);
                }
            )
        );

        await session.Session.CommitTransactionAsync();

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

    private static async Task CalcPartialSrLevel(IEnumerable<string> symbols) {
        await SrLevelController.UpdateAll(SrLevelComputer.CalcLevels(symbols));
    }

    public static async Task CalcPartial(IList<string> symbols, int limit) {
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
                .Select(
                    async r => {
                        try {
                            var history = groupedHistory[r.Symbol][r.Period];
                            var cachedCalculated = groupedCalculated[(r.Symbol, r.Period)];

                            var calculated = (await HistoryDataComputer.CalcPartial(
                                    history,
                                    cachedCalculated,
                                    r.Period
                                ))
                                .ToImmutableArray();
                            await CalculatedDataController.UpdateByEpoch(calculated);
                        } catch (InvalidOperationException e) {
                            Log.Error(
                                e,
                                "Error when attempting partial calculation for {Symbol} @ {Period}",
                                r.Symbol,
                                r.Period
                            );
                            throw new RpcException(
                                new Status(
                                    StatusCode.NotFound,
                                    $"Unable to perform partial calculation for {r.Symbol} @ {r.Period}, symbol might be unavailable"
                                )
                            );
                        }
                    }
                )
                .Concat(new[] { CalcPartialSrLevel(symbols) })
        );

        Log.Information(
            "Completed calc partial request {@Symbols} x {@Periods}",
            symbols,
            PxConfigController.Config.Periods.Select(r => r.PeriodMin)
        );
    }

    public static async Task CalcLast(string symbol) {
        await Task.WhenAll(
            PxConfigController.Config.Periods.Select(
                r => Task.Run(
                    async () => {
                        var data = CalculatedDataController.GetData(symbol, r.PeriodMin, 2).ToImmutableArray();

                        if (data.IsEmpty) {
                            throw new RpcException(
                                new Status(
                                    StatusCode.NotFound,
                                    $"{symbol} @ {r.PeriodMin} does not have calculated data available"
                                )
                            );
                        }

                        var calculated = HistoryDataComputer.CalcLast(data[^1], data[^2]);
                        await CalculatedDataController.UpdateByEpoch(ImmutableArray.Create(new[] { calculated }));
                    }
                )
            )
        );

        Log.Information(
            "Completed calc last request of {Symbol} @ {@Periods}",
            symbol,
            PxConfigController.Config.Periods.Select(r => r.PeriodMin)
        );
    }
}