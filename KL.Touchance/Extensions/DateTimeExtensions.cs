using KL.Common.Extensions;

namespace KL.Touchance.Extensions;


public static class DateTimeExtensions {
    // ReSharper disable once StringLiteralTypo
    public static string ToTouchanceHourlyPrecision(this DateTime dateTime) {
        return dateTime.ToString("yyyyMMddHH");
    }

    // ReSharper disable once StringLiteralTypo
    public static DateTime FromTouchanceHourlyPrecision(this string dateTime) {
        return dateTime.ToUtcDateTime("yyyyMMddHH");
    }
}