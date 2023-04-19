namespace KL.Touchance.Handlers;


internal class MinuteChangedHandler {
    private readonly Dictionary<string, DateTime> _prevTimestamp = new();

    internal bool IsMinuteChanged(string symbol, DateTime timestamp) {
        if (_prevTimestamp.TryGetValue(symbol, out var prevTimestamp) && prevTimestamp.Minute == timestamp.Minute) {
            return false;
        }

        _prevTimestamp[symbol] = timestamp;
        return true;
    }
}