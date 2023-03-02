using KL.Common.Extensions;
using KL.Touchance.Interfaces;

namespace KL.Touchance.Extensions;


public static class TimestampExtensions {
    public static DateTime GetTimestamp(this ITimestamp timestamp, int minOffset = 0) {
        var date = timestamp.Date == 0
            ?
            // ReSharper disable once StringLiteralTypo
            $"{timestamp.Time:D6} {timestamp.Date}".ToUtcDateTime("HHmmss yyyyMMdd")
            :
            // ReSharper disable once StringLiteralTypo
            $"{timestamp.Date} {timestamp.Time:D6}".ToUtcDateTime("yyyyMMdd HHmmss");

        return date.AddMinutes(minOffset);
    }
}