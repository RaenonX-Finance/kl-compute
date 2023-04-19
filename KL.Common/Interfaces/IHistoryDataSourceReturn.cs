using KL.Common.Events;
using KL.Common.Models;

namespace KL.Common.Interfaces; 


public interface IHistoryDataSourceReturn {
    public IHistoryMetadata Metadata { get; init; }

    public IList<IHistoryDataEntry> Data { get; init; }

    public bool IsSubscription { get; init; }

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
            },
            IsTriggeredByHistory = true
        };
    }
}