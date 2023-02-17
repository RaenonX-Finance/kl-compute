using System.Collections.Immutable;
using KL.Common.Interfaces;

namespace KL.Common.Events;


public class HistoryEventArgs : EventArgs {
    public required IHistoryMetadata Metadata { get; init; }

    public required IImmutableList<IHistoryDataEntry> Data { get; init; }

    public required bool IsSubscription { get; init; }
}