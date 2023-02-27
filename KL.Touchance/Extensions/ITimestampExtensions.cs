using KL.Common.Extensions;
using KL.Touchance.Interfaces;

namespace KL.Touchance.Extensions;


public static class TimestampExtensions {
    public static DateTime GetTimestamp(this ITimestamp timestamp, int minOffset = 0) {
        // ReSharper disable once StringLiteralTypo
        return $"{timestamp.Date} {timestamp.Time:D6}".ToUtcDateTime("yyyyMMdd HHmmss").AddMinutes(minOffset);
    }
}