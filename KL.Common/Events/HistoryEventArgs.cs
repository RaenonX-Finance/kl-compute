using KL.Common.Interfaces;
using KL.Common.Models;

namespace KL.Common.Events;


public class HistoryEventArgs : EventArgs {
    public required IHistoryMetadata Metadata { get; init; }

    public required IList<IHistoryDataEntry> Data { get; init; }

    public required bool IsSubscription { get; init; }

    public RealtimeEventArgs ToRealtimeEventArgs() {
        if (!IsSubscription) {
            throw new InvalidOperationException("History event is not a subscription, invalid realtime event");
        }

        var lastBar = Data[^1];

        return new RealtimeEventArgs {
            Symbol = Metadata.Symbol,
            Data = new PxRealtimeModel {
                Open = lastBar.Open,
                High = lastBar.High,
                Low = lastBar.Low,
                Close = lastBar.Close
            }
        };
    }
}