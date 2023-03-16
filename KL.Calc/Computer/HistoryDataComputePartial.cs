using System.Collections.Immutable;
using KL.Calc.Computer.Models;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Models;

namespace KL.Calc.Computer;


public class CalculatePartialBaseData {
    public required ImmutableArray<GroupedHistoryDataModel> History { get; init; }

    public required ImmutableArray<CalculatedDataModel> Calculated { get; init; }
}

public class MatchingIndex {
    public required Index OnHistory { get; init; }

    public required Index PrevOfFirstHistoryOnCalculated { get; init; }
}

public static partial class HistoryDataComputer {
    private static CalculatePartialBaseData GetBaseDataForPartial(
        IEnumerable<GroupedHistoryDataModel> groupedHistory,
        IEnumerable<CalculatedDataModel> calculatedData
    ) {
        var calculated = calculatedData.ToImmutableArray();

        if (calculated.IsEmpty()) {
            Log.Error("Attempted to calculate partial data but `calculatedData` is empty");
            throw new InvalidOperationException("Empty calculated data");
        }

        var earliestInCalc = calculated.Select(r => r.Date).Min();

        var history = groupedHistory
            .Where(r => r.Date >= earliestInCalc)
            .ToImmutableArray();

        if (history.Length >= 2) {
            return new CalculatePartialBaseData {
                History = history,
                Calculated = calculated
            };
        }

        var historyInfo = history.Length > 0 ? $"{history[0].Date} ~ {history[^1].Date}" : "(Empty)";
        var calculatedInfo = calculated.Length > 0 ? $"{calculated[0].Date} ~ {calculated[^1].Date}" : "(Empty)";

        Log.Error(
            "History data length needs to be > 2 for partial calculation\n\tHistory: {History}\n\tCalculated: {calculated}",
            historyInfo,
            calculatedInfo
        );
        throw new InvalidOperationException("History data length needs to be > 2 for partial calculation");
    }

    private static MatchingIndex GetFirstMatchingIndex(
        IReadOnlyCollection<GroupedHistoryDataModel> history,
        IReadOnlyList<CalculatedDataModel> calculated
    ) {
        var lastCalculated = calculated[^1];

        var revHistory = history.Reverse().ToArray();
        var revCalculated = calculated.Reverse().ToArray();

        var idxFirstSync = revHistory
            .TakeWhile(r => r.Date != lastCalculated.Date)
            .Count();

        if (idxFirstSync == history.Count) {
            Log.Error(
                "Base index on history targeting last available calculated data not found "
                + "({Count} history data searched)",
                history.Count
            );
            throw new IndexOutOfRangeException(
                "Base index on history targeting last available calculated data not found"
            );
        }

        var revHistoryFirstSync = revHistory[idxFirstSync..];

        var idxFirstMatch = revHistoryFirstSync
            .Zip(revCalculated)
            .TakeWhile(r => !r.First.EqualsInPrice(r.Second))
            .Count();

        // ReSharper disable once InvertIf
        if (idxFirstMatch == revHistoryFirstSync.Length) {
            Log.Error(
                "Matching calculated data not found, try feeding more calculated data "
                + "({Count} truncated history data searched)",
                revHistoryFirstSync.Length
            );
            throw new IndexOutOfRangeException("Matching calculated data not found, try feeding more calculated data");
        }

        // +1 because if `index` found the match on first, the desired index is `^1` instead of `^0`
        return new MatchingIndex {
            OnHistory = ^(idxFirstSync + idxFirstMatch + 1),
            PrevOfFirstHistoryOnCalculated = ^(idxFirstMatch + 2)
        };
    }

    private static IList<double?> CalculateEmaFromBase<TData>(
        ICollection<TData> dataList,
        Func<TData, double?> getCurrentValue,
        int period,
        double? startingEma
    ) {
        if (startingEma is null) {
            Log.Warning("Attempt to calculate EMA with null starting point (EMA {Period})", period);
            return Enumerable.Repeat<double?>(null, dataList.Count).ToArray();
        }

        var emaList = new List<double?> { startingEma };

        foreach (var data in dataList) {
            var emaResult = CalculateSingleEma(getCurrentValue(data), emaList[^1], period);

            emaList.Add(emaResult);
        }

        // Starting EMA shouldn't be included in the return
        return emaList.Skip(1).ToArray();
    }

    private static IList<Dictionary<int, double?>> CalculatePartialEma(
        ICollection<GroupedHistoryDataModel> history,
        CalculatedDataModel startingCalculated
    ) {
        var emaList = history.Select(_ => new Dictionary<int, double?>()).ToArray();

        foreach (var emaPeriod in PxConfigController.EmaPeriods) {
            var emaSingle = CalculateEmaFromBase(
                history,
                r => (double)r.Close,
                emaPeriod,
                startingCalculated.Ema[emaPeriod]
            );

            foreach (var (ema, idx) in emaSingle.Select((r, idx) => (r, idx))) {
                emaList[idx][emaPeriod] = ema;
            }
        }

        return emaList;
    }

    private static async Task<TiePointDataCollection> CalculatePartialTiePoint(
        IList<GroupedHistoryDataModel> history,
        CalculatedDataModel startingCalculated
    ) {
        var marketDateHigh = Task.Run(
            () => history.GroupedCumulativeMax(
                    r => r.MarketDate,
                    r => r.High,
                    new Dictionary<DateOnly, decimal> {
                        { startingCalculated.MarketDate, startingCalculated.MarketDateHigh }
                    }
                )
                .ToArray()
        );
        var marketDateLow = Task.Run(
            () => history.GroupedCumulativeMin(
                    r => r.MarketDate,
                    r => r.Low,
                    new Dictionary<DateOnly, decimal> {
                        { startingCalculated.MarketDate, startingCalculated.MarketDateLow }
                    }
                )
                .ToArray()
        );

        return new TiePointDataCollection {
            MarketDateHigh = await marketDateHigh,
            MarketDateLow = await marketDateLow,
            TiePoint = (await marketDateHigh).Zip(await marketDateLow)
                .Select(r => (r.First + r.Second) / 2)
                .ToArray()
        };
    }

    private static IList<CandleDirectionDataPoint> CalculatePartialCandleDirection(
        IList<Dictionary<int, double?>> emaList,
        CalculatedDataModel startingCalculated
    ) {
        var candleConfig = PxConfigController.Config.CandleDirection;

        var macdFast = emaList.Select(r => r[candleConfig.Fast]);
        var macdSlow = emaList.Select(r => r[candleConfig.Slow]);
        var macd = macdFast.Zip(macdSlow).Select(r => r.First - r.Second).ToArray();
        var signal = CalculateEmaFromBase(
            macd,
            r => r,
            candleConfig.Signal,
            startingCalculated.MacdSignal
        );
        var histogram = macd.Zip(signal).Select(r => r.First - r.Second);

        return signal.Zip(histogram)
            .Select(
                r => new CandleDirectionDataPoint {
                    Direction = r.Second is null
                        ? 0
                        : r.Second > 0
                            ? 1
                            : -1,
                    Signal = r.First
                }
            )
            .ToArray();
    }
}