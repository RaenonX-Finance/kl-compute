using KL.Common.Extensions;

namespace KL.Touchance.Extensions;


public static class DateTimeExtensions {
    public static string ToTouchanceHourlyPrecision(this DateTime dateTime) {
        return dateTime.ToString("yyyyMMddHH");
    }

    public static DateTime FromTouchanceHourlyPrecision(this string dateTime) {
        return dateTime.ToUtcDateTime("yyyyMMddHH");
    }

    public static DateTime FromTouchanceRealtime(this string dateTime) {
        return dateTime.ToUtcDateTime("yyyyMMdd HHmmssffffff");
    }

    public static DateTime ToTouchanceDatetime(this DateTime dateTime) {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);
    }
}