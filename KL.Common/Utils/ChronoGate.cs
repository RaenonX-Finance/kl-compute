namespace KL.Common.Utils;


public class ChronoGate<T> where T : notnull {
    private readonly Dictionary<T, DateTime> _nextOpenOfGates = new();
    
    public bool IsGateOpened(T key, int gateOpenGapMs, out DateTime nextOpen) {
        var now = DateTime.UtcNow;
        if (!_nextOpenOfGates.TryGetValue(key, out var gateOpen)) {
            nextOpen = _nextOpenOfGates[key] = now.AddMilliseconds(gateOpenGapMs);
            return true;
        }

        var gateOpened = now > gateOpen;
        if (!gateOpened) {
            nextOpen = gateOpen;
            return false;
        }

        nextOpen = _nextOpenOfGates[key] = now.AddMilliseconds(gateOpenGapMs);

        return true;
    }
}