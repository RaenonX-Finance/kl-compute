using System.Numerics;

namespace KL.Common.Utils;


public static class NumberHelper {
    public static IEnumerable<TNumber> RangeWithStep<TNumber>(TNumber start, TNumber end, TNumber step)
        where TNumber : INumber<TNumber> {
        for (var i = start; i <= end; i += step) {
            yield return i;
        }
    }

    public static int Mod(int num, int mod) {
        var r = num % mod;
        return r < 0 ? r + mod : r;
    }
}