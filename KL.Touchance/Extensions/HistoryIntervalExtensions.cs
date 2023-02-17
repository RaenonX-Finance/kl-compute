using KL.Common.Enums;

namespace KL.Touchance.Extensions;


public static class HistoryIntervalExtensions {
    public static string GetTouchanceType(this HistoryInterval interval) {
        return interval switch {
            HistoryInterval.Minute => "1K",
            HistoryInterval.Daily => "DK",
            _ => throw new ArgumentException($"History interval {interval} does not have corresponding Touchance type")
        };
    }
}