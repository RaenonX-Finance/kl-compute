using KL.Common.Models.Config;

namespace KL.Common.Extensions;


public static class ProductCategoryExtensions {
    public static DateOnly ToMarketDate(this DateTime timestamp, MarketDateCutoffModel cutoff) {
        timestamp = timestamp.ToTimezone(cutoff.Timezone);

        var date = DateOnly.FromDateTime(timestamp);
        var time = TimeOnly.FromDateTime(timestamp);

        return date.AddBusinessDay((time > cutoff.Time ? 0 : -1) + cutoff.OffsetOnCutoff);
    }
}