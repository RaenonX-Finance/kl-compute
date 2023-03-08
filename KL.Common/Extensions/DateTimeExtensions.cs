using System.Globalization;

namespace KL.Common.Extensions;


public static class DateTimeExtensions {
    public static string ToShortIso8601(this DateTime datetime) {
        return datetime.ToString("yyyy-MM-dd'T'HH:mmK", CultureInfo.InvariantCulture);
    }

    public static DateTime ToDateTime(this long epochSec) {
        return DateTimeOffset.FromUnixTimeSeconds(epochSec).DateTime;
    }

    public static long ToEpochSeconds(this DateTime datetime) {
        return ((DateTimeOffset)datetime).ToUnixTimeSeconds();
    }

    private static bool IsWorkday(this DayOfWeek dayOfWeek) {
        switch (dayOfWeek) {
            case DayOfWeek.Monday:
            case DayOfWeek.Tuesday:
            case DayOfWeek.Wednesday:
            case DayOfWeek.Thursday:
            case DayOfWeek.Friday:
                return true;
            case DayOfWeek.Saturday:
            case DayOfWeek.Sunday:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek.ToString());
        }
    }

    public static DateOnly AddBusinessDay(this DateOnly date, int offset) {
        var singleOffset = Math.Sign(offset);

        while (offset != 0) {
            do {
                date = date.AddDays(singleOffset);
            } while (!date.DayOfWeek.IsWorkday());

            offset -= singleOffset;
        }

        return date;
    }

    public static DateTime ToTimezone(this DateTime date, TimeZoneInfo timezone) {
        return TimeZoneInfo.ConvertTime(date, timezone);
    }

    public static DateTime ToTimezoneFromUtc(this DateTime date, TimeZoneInfo timezone) {
        return TimeZoneInfo.ConvertTimeFromUtc(date, timezone);
    }

    public static DateTime FromTimezoneToUtc(this DateTime date, TimeZoneInfo timezone) {
        return TimeZoneInfo.ConvertTime(date, timezone, TimeZoneInfo.Utc);
    }
}