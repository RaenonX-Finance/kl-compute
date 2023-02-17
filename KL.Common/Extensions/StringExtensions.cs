using System.Globalization;

namespace KL.Common.Extensions;


public static class StringExtensions {
    public static DateTime ToUtcDateTime(this string datetimeString, string format) {
        return DateTime.ParseExact(
            datetimeString,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal
        );
    }
}