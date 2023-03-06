using KL.Common.Events;

namespace KL.Touchance.Handlers;


public class MinuteChangedHandler {
    private readonly Dictionary<string, DateTime> _prevTimestamp = new();

    public required TouchanceClient Client { get; init; }

    public void CheckMinuteChangedEvent(string symbol, DateTime timestamp) {
        if (_prevTimestamp.TryGetValue(symbol, out var prevTimestamp) && prevTimestamp.Minute == timestamp.Minute) {
            return;
        }

        _prevTimestamp[symbol] = timestamp;
        Client.OnMinuteChanged(new MinuteChangeEventArgs { Symbol = symbol, Timestamp = timestamp });
    }
}