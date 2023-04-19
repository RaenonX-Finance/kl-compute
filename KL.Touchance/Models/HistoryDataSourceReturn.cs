using KL.Common.Interfaces;

namespace KL.Touchance.Models;


public class HistoryDataSourceReturn : IHistoryDataSourceReturn {
    public required IHistoryMetadata Metadata { get; init; }

    public required IList<IHistoryDataEntry> Data { get; init; }

    public required bool IsSubscription { get; init; }
}