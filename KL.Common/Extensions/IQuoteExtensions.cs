using Skender.Stock.Indicators;

namespace KL.Common.Extensions;


public static class QuoteExtensions {
    public static bool EqualsInPrice(this IQuote @base, IQuote comparison) {
        // Order by (Close, High, Low, Date, Open) according to the likelihood of difference
        // This could give tiny amount of performance improvement because of short-circuiting
        return @base.Close == comparison.Close
               && @base.High == comparison.High
               && @base.Low == comparison.Low
               && @base.Date == comparison.Date
               && @base.Open == comparison.Open;
    }
}