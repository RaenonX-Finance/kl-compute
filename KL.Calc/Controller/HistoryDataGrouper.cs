using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Models;
using ILogger = Serilog.ILogger;

namespace KL.Calc.Controller;


public static class HistoryDataGrouper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataGrouper));

    private static IEnumerable<GroupedHistoryDataModel> GetGroupedFromData(
        IEnumerable<HistoryDataModel> historyData,
        string symbol,
        HistoryInterval interval,
        int periodMin
    ) {
        /* Using C# native grouping because given 171473 data:
            - C# Native Projection + GroupBy takes 850 ms
            - Mongo Agg Projection + C# GroupBy takes 1000 ms
            - Mongo Agg Projection + GroupBy takes 1500 ms
         */
        if (
            (periodMin == 1 && interval == HistoryInterval.Minute)
            || (periodMin == 1440 && interval == HistoryInterval.Daily)
        ) {
            return historyData
                .Select(r => r.ToGroupedHistoryDataModel())
                .OrderBy(r => r.EpochSecond);
        }

        var periodSec = periodMin * 60;

        return historyData
            .Select(
                r => new HistoryDataModel {
                    Timestamp = r.Timestamp,
                    Open = r.Open,
                    High = r.High,
                    Low = r.Low,
                    Close = r.Low,
                    Volume = r.Volume,
                    Symbol = r.Symbol,
                    Interval = r.Interval,
                    EpochSecond = r.EpochSecond / periodSec * periodSec,
                    MarketDate = r.MarketDate
                }
            )
            .GroupBy(r => r.EpochSecond)
            .Select(group => GroupedHistoryDataModel.FromGroupingByEpochSec(group, symbol))
            .OrderBy(r => r.EpochSecond);
    }

    private static async Task<IDictionary<int, IEnumerable<GroupedHistoryDataModel>>> GetGroupedSingleSymbol(
        string symbol,
        IList<int> periodMinList,
        Func<HistoryInterval, IEnumerable<HistoryDataModel>> queryByInterval,
        int? limit = null
    ) {
        /* Using C# native grouping because given 171473 data:
            - C# Native Projection + GroupBy takes 850 ms
            - Mongo Agg Projection + C# GroupBy takes 1000 ms
            - Mongo Agg Projection + GroupBy takes 1500 ms
         */
        var dataTasks = periodMinList
            .Select(periodMin => periodMin.GetHistoryInterval())
            .Distinct()
            .Select(
                interval => Task.Run(
                    () => (
                        Interval: interval, Data: queryByInterval(interval).ToArray()
                    )
                )
            );

        var dataDict = (await Task.WhenAll(dataTasks).ConfigureAwait(false))
            .ToDictionary(r => r.Interval, r => r.Data);

        return periodMinList.ToDictionary(
            periodMin => periodMin,
            periodMin => {
                var interval = periodMin.GetHistoryInterval();
                var data = dataDict[interval];
                var groupedMaxLength = limit ?? data.Length;

                var grouped = GetGroupedFromData(
                    data,
                    symbol,
                    interval,
                    periodMin
                );

                if (groupedMaxLength != data.Length) {
                    grouped = grouped.TakeLast(groupedMaxLength);
                }

                return grouped;
            }
        );
    }

    public static async Task<
        IDictionary<string, IDictionary<int, IEnumerable<GroupedHistoryDataModel>>>
    > GetGroupedDictOfAll(
        IList<string> symbols,
        IList<int> periodMinList
    ) {
        Log.Information("Get grouped history of {@Symbols} x {@PeriodMinList}", symbols, periodMinList);
        var groupedDictTasks = symbols
            .Select(
                symbol => Task.Run(
                    async () => (
                        Symbol: symbol,
                        GroupedData: await GetGroupedSingleSymbol(
                            symbol,
                            periodMinList,
                            interval => HistoryDataController.GetAll(symbol, interval)
                        )
                    )
                )
            );
        return (await Task.WhenAll(groupedDictTasks).ConfigureAwait(false))
            .ToDictionary(r => r.Symbol, r => r.GroupedData);
    }

    public static async Task<
        IDictionary<string, IDictionary<int, IEnumerable<GroupedHistoryDataModel>>>
    > GetGroupedDictOfLastN(
        IList<string> symbols,
        IList<int> periodMinList,
        int limit
    ) {
        Log.Information(
            "Get grouped history of {@Symbols} x {@PeriodMinList} with {Limit} data only",
            symbols,
            periodMinList,
            limit
        );

        var groupedDictTasks = symbols
            .Select(
                symbol => Task.Run(
                    async () => (
                        Symbol: symbol,
                        GroupedData: await GetGroupedSingleSymbol(
                            symbol,
                            periodMinList,
                            interval => {
                                // Period of 5 with 100 data needs 500 data of 1K
                                var maxPeriodOfInterval = periodMinList
                                    .Where(r => interval == HistoryInterval.Minute ? r < 1440 : r >= 1440)
                                    .Max();

                                return HistoryDataController.GetLastN(symbol, interval, limit * maxPeriodOfInterval);
                            },
                            limit
                        )
                    )
                )
            );

        return (await Task.WhenAll(groupedDictTasks).ConfigureAwait(false))
            .ToDictionary(r => r.Symbol, r => r.GroupedData);
    }
}