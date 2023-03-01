using KL.Common.Controllers;
using KL.Common.Interfaces;
using KL.Common.Models;

namespace KL.Calc.Computer;


public static partial class HistoryDataComputer {
    private static void CalculateLastDiff(CalculatedDataModel last1) {
        last1.Diff = last1.Close - last1.Open;
    }

    private static void CalculateLastTiePoint(CalculatedDataModel last1, IGroupedHistoryDataModel last2) {
        var currentClose = last1.Close;

        if (last1.MarketDate != last2.MarketDate) {
            last1.MarketDateHigh = currentClose;
            last1.MarketDateLow = currentClose;
            last1.TiePoint = currentClose;
        } else {
            last1.MarketDateHigh = decimal.Max(last1.MarketDateHigh, currentClose);
            last1.MarketDateLow = decimal.Min(last1.MarketDateLow, currentClose);
            last1.TiePoint = (last1.MarketDateHigh + last1.MarketDateLow) / 2;
        }
    }

    private static double CalculateSingleEma(double? currentValue, double? prevEma, int period) {
        var current = currentValue ?? double.NaN;
        var prev = prevEma ?? double.NaN;
        if (double.IsNaN(prev)) {
            return double.NaN;
        }

        var k = 2d / (period + 1);
        return current * k + prev * (1 - k);
    }

    private static void CalculateLastEma(CalculatedDataModel last1, CalculatedDataModel last2, int period) {
        var prevEma = last2.Ema[period] ?? double.NaN;

        if (double.IsNaN(prevEma)) {
            last1.Ema[period] = double.NaN;
            return;
        }

        last1.Ema[period] = CalculateSingleEma((double)last1.Close, prevEma, period);
    }

    private static void CalculateLastCandleDirection(CalculatedDataModel last1, CalculatedDataModel last2) {
        var calcPeriod = PxConfigController.Config.CandleDirection;

        var macdFast = last1.Ema[calcPeriod.Fast];
        var macdSlow = last1.Ema[calcPeriod.Slow];

        var macd = macdFast - macdSlow;
        var signal = CalculateSingleEma(macd, last2.MacdSignal, calcPeriod.Signal);
        var hist = macd - signal;

        last1.MacdSignal = signal;
        last1.CandleDirection = double.IsNaN(hist ?? double.NaN)
            ? 0
            : hist > 0
                ? 1
                : -1;
    }
}