using Skender.Stock.Indicators;

namespace KL.Common.Interfaces;


public interface IGroupedHistoryDataModel : IQuote {
    public long EpochSecond { get; init; }

    public DateOnly MarketDate { get; init; }
}