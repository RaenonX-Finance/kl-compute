using System.Numerics;

namespace KL.Common.Utils;


public static class NumberHelper {
    public static IEnumerable<TNumber> IncreasingRangeWithStep<TNumber>(TNumber start, TNumber end, TNumber step)
        where TNumber : INumber<TNumber> {
        if (step < TNumber.Zero) {
            throw new InvalidOperationException($"`step` ({step}) for an increasing range should not be < 0");
        }
        
        for (var i = start; i <= end; i += step) {
            yield return i;
        }
    }
    
    public static IEnumerable<TNumber> DecreasingRangeWithStep<TNumber>(TNumber start, TNumber end, TNumber step)
        where TNumber : INumber<TNumber> {
        if (step > TNumber.Zero) {
            throw new InvalidOperationException($"`step` ({step}) for an decreasing range should not be > 0");
        }
        
        for (var i = start; i >= end; i += step) {
            yield return i;
        }
    }

    public static int Mod(int num, int mod) {
        var r = num % mod;
        return r < 0 ? r + mod : r;
    }
}