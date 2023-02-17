using System.Diagnostics;

namespace KL.Common.Extensions;


public static class NumberExtensions {
    public static double GetElapsedMs(this long stopwatchTimestamp) {
        return Stopwatch.GetElapsedTime(stopwatchTimestamp).TotalMilliseconds;
    }
}