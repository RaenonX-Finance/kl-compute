using KL.Common.Controllers;
using KL.Common.Enums;

namespace KL.Calc.Computer;


public static class MomentumComputer {
    private static readonly int[] DataInterval = { 1, 3, 5 };

    public static async Task<Momentum> CalcMomentum(string symbol) {
        var lastPxSeriesRev = await RedisLastPxController.GetRev(symbol, 51);

        var momentum = DataInterval
            .Select(r => CalcMomentumPair(symbol, lastPxSeriesRev.ToArray(), r * 5, r * 10))
            .Sum(r => (int)r);

        return (Momentum)momentum;
    }

    private static Momentum CalcMomentumPair(string symbol, double[] pxRev, int shortPeriod, int longPeriod) {
        if (shortPeriod > pxRev.Length) {
            throw new InvalidOperationException(
                $"Data not enough to calculate short period MA of {symbol} ({shortPeriod} / {pxRev.Length})"
            );
        }

        if (longPeriod > pxRev.Length) {
            throw new InvalidOperationException(
                $"Data not enough to calculate long period MA of {symbol} ({longPeriod} / {pxRev.Length})"
            );
        }

        var last = pxRev[0];
        var avgShort = pxRev[..shortPeriod].Average();
        var avgLong = pxRev[..longPeriod].Average();

        if (last > avgShort && avgShort > avgLong) {
            return Momentum.Long1;
        }

        if (last < avgShort && avgShort < avgLong) {
            return Momentum.Short1;
        }

        return Momentum.Neutral;
    }
}