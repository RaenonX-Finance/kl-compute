using KL.Common.Events;

namespace KL.Touchance.Handlers;


internal class MinuteChangedHandler {
    private readonly Dictionary<string, DateTime> _prevTimestamp = new();

    internal required TouchanceClient Client { get; init; }

    internal void CheckMinuteChangedEvent(string symbol, DateTime timestamp) {
        if (_prevTimestamp.TryGetValue(symbol, out var prevTimestamp) && prevTimestamp.Minute == timestamp.Minute) {
            return;
        }

        _prevTimestamp[symbol] = timestamp;
        Client.OnMinuteChanged(new MinuteChangeEventArgs { Symbol = symbol, Timestamp = timestamp });
    }
}