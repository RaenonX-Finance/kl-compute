using KL.Calc.Computer.Models;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Extensions;
using Skender.Stock.Indicators;

namespace KL.Calc.Computer;


public static partial class HistoryDataComputer {
    private static IList<decimal> CalculateAllDiff(IEnumerable<GroupedHistoryDataModel> groupedHistory) {
        return groupedHistory.Select(r => r.Close - r.Open).ToArray();
    }

    private static IList<Dictionary<int, double?>> CalculateAllEma(IList<GroupedHistoryDataModel> groupedHistory) {
        // Making EMA calculation async doesn't help much
        var emaList = groupedHistory.Select(_ => new Dictionary<int, double?>()).ToArray();

        foreach (var emaPeriod in PxConfigController.EmaPeriods)
        foreach (var (emaResult, idx) in groupedHistory.GetEma(emaPeriod).Select((ema, idx) => (ema, idx))) {
            emaList[idx][emaPeriod] = emaResult.Ema;
        }

        return emaList;
    }

    private static async Task<TiePointDataCollection> CalculateAllTiePoint(
        IList<GroupedHistoryDataModel> groupedHistory
    ) {
        var marketDateHigh = Task.Run(
            () => groupedHistory.GroupedCumulativeMax(r => r.MarketDate, r => r.High).ToArray()
        );
        var marketDateLow = Task.Run(
            () => groupedHistory.GroupedCumulativeMin(r => r.MarketDate, r => r.Low).ToArray()
        );

        return new TiePointDataCollection {
            MarketDateHigh = await marketDateHigh,
            MarketDateLow = await marketDateLow,
            TiePoint = (await marketDateHigh).Zip(await marketDateLow)
                .Select(r => (r.First + r.Second) / 2)
                .ToArray()
        };
    }

    private static IList<CandleDirectionDataPoint> CalculateAllCandleDirection(
        IEnumerable<GroupedHistoryDataModel> groupedHistory
    ) {
        var candleConfig = PxConfigController.Config.CandleDirection;

        return groupedHistory
            .GetMacd(
                candleConfig.Fast,
                candleConfig.Slow,
                candleConfig.Signal
            )
            .Select(
                r => new CandleDirectionDataPoint {
                    Direction = r.Histogram is null
                        ? 0
                        : r.Histogram > 0
                            ? 1
                            : -1,
                    Signal = r.Signal
                }
            )
            .ToArray();
    }
}