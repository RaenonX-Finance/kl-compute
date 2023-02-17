using KL.Common.Extensions;
using KL.Touchance.Interfaces;

namespace KL.Touchance.Extensions;


public static class TimestampExtensions {
    public static DateTime GetTimestamp(this ITimestamp timestamp) {
        // ReSharper disable once StringLiteralTypo
        return $"{timestamp.Date} {timestamp.Time:D6}".ToUtcDateTime("yyyyMMdd HHmmss");
    }
}