using KL.Calc.Computer.Models;
using KL.Common.Interfaces;
using KL.Common.Models;

namespace KL.Calc.Models;


public class GroupedHistoryDataModel : IGroupedHistoryDataModel {
    public required string Symbol { get; init; }

    public required DateTime Date { get; init; }

    public required decimal Open { get; init; }

    public required decimal High { get; init; }

    public required decimal Low { get; init; }

    public required decimal Close { get; init; }

    public required decimal Volume { get; init; }

    public required long EpochSecond { get; init; }

    public required DateOnly MarketDate { get; init; }

    public CalculatedDataModel ToCalculated(
        int periodMin,
        decimal diff,
        decimal marketDateHigh,
        decimal marketDateLow,
        decimal tiePoint,
        CandleDirectionDataPoint candleDirection,
        Dictionary<int, double?> ema
    ) {
        return new CalculatedDataModel {
            Symbol = Symbol,
            PeriodMin = periodMin,
            Date = Date,
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            MarketDate = MarketDate,
            EpochSecond = EpochSecond,
            Diff = diff,
            MarketDateHigh = marketDateHigh,
            MarketDateLow = marketDateLow,
            TiePoint = tiePoint,
            MacdSignal = candleDirection.Signal,
            CandleDirection = candleDirection.Direction,
            Ema = ema
        };
    }

    public static GroupedHistoryDataModel FromGroupingByEpochSec(
        IGrouping<long, HistoryDataModel> group,
        string symbol
    ) {
        return new GroupedHistoryDataModel {
            Date = group.Select(r => r.Timestamp).First(),
            Open = group.Select(r => r.Open).First(),
            High = group.Select(r => r.High).Max(),
            Low = group.Select(r => r.Low).Min(),
            Close = group.Select(r => r.Close).Last(),
            Volume = group.Select(r => r.Volume).Sum(),
            Symbol = symbol,
            EpochSecond = group.Select(r => r.EpochSecond).First(),
            MarketDate = group.Select(r => r.MarketDate).First()
        };
    }
}

public static class GroupedHistoryDataModelHelper {
    public static GroupedHistoryDataModel ToGroupedHistoryDataModel(this HistoryDataModel r) {
        return new GroupedHistoryDataModel {
            Date = r.Timestamp,
            Open = r.Open,
            High = r.High,
            Low = r.Low,
            Close = r.Close,
            Volume = r.Volume,
            Symbol = r.Symbol,
            EpochSecond = r.EpochSecond,
            MarketDate = r.MarketDate
        };
    }
}