using System.Diagnostics;
using KL.Calc.Models;
using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Models;
using ILogger = Serilog.ILogger;

namespace KL.Calc.Computer;


public static partial class HistoryDataComputer {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataComputer));

    public static CalculatedDataModel CalcLast(CalculatedDataModel last1, CalculatedDataModel last2) {
        // Level 1 indicator
        CalculateLastDiff(last1);
        CalculateLastTiePoint(last1, last2);

        foreach (var emaPeriod in PxConfigController.EmaPeriods) {
            CalculateLastEma(last1, last2, emaPeriod);
        }

        // Level 2 indicator (indicator on indicator)
        CalculateLastCandleDirection(last1, last2);

        return last1;
    }

    public static async Task<IEnumerable<CalculatedDataModel>> CalcPartial(
        (string Symbol, int Period) c,
        IEnumerable<GroupedHistoryDataModel> groupedHistory,
        IEnumerable<CalculatedDataModel> calculatedData
    ) {
        var start = Stopwatch.GetTimestamp();

        // Get base data needed
        var baseData = GetBaseDataForPartial(groupedHistory, calculatedData);
        var baseIndex = GetFirstMatchingIndex(baseData.History, baseData.Calculated);
        var history = baseData.History[baseIndex.OnHistory..];
        var prevCalculated = baseData.Calculated[baseIndex.PrevOfFirstHistoryOnCalculated];

        // Level 1 indicator (Task creation)
        var calcPartialDiffTask = Task.Run(() => CalculateAllDiff(history));
        var calcPartialTiePointTask = CalculatePartialTiePoint(history, prevCalculated);
        var calcPartialEmaTask = Task.Run(() => CalculatePartialEma(history, prevCalculated));

        // Level 2 indicator (indicator on indicator)
        var calcPartialCandleDirection = Task.Run(
            async () => CalculatePartialCandleDirection(await calcPartialEmaTask, prevCalculated)
        );

        // Level 1 indicator
        var diff = await calcPartialDiffTask;
        var tiePointData = await calcPartialTiePointTask;
        var emaDict = await calcPartialEmaTask;

        // Level 2 indicator (indicator on indicator)
        var candleDirection = await calcPartialCandleDirection;

        Log.Information(
            "Calculated partial indicator for {Symbol} @ {PeriodMin} ({Count}) in {Elapsed:0.00} ms",
            c.Symbol,
            c.Period,
            history.Length,
            start.GetElapsedMs()
        );

        return history.Select(
            (r, idx) => r.ToCalculated(
                c.Period,
                diff[idx],
                tiePointData.MarketDateHigh[idx],
                tiePointData.MarketDateLow[idx],
                tiePointData.TiePoint[idx],
                candleDirection[idx],
                emaDict[idx]
            )
        );
    }

    public static async Task<IEnumerable<CalculatedDataModel>> CalcAll(
        IList<GroupedHistoryDataModel> historyData,
        int periodMin
    ) {
        var start = Stopwatch.GetTimestamp();

        // Level 1 indicator (Task creation)
        var calcAllDiffTask = Task.Run(() => CalculateAllDiff(historyData));
        var calcTiePointTask = CalculateAllTiePoint(historyData);
        var calcAllEmaTask = Task.Run(() => CalculateAllEma(historyData));

        // Level 2 indicator (Task creation)
        var calcCandleDirTask = Task.Run(() => CalculateAllCandleDirection(historyData));

        // Level 1 indicator
        var diff = await calcAllDiffTask;
        var tiePointData = await calcTiePointTask;
        var emaDict = await calcAllEmaTask;

        // Level 2 indicator (indicator on indicator)
        var candleDirection = await calcCandleDirTask;

        var firstHistory = historyData[0];
        Log.Information(
            "Calculated all indicators of {Symbol} @ {PeriodMin} ({Count}) in {Elapsed:0.00} ms",
            firstHistory.Symbol,
            periodMin,
            historyData.Count,
            start.GetElapsedMs()
        );
        return historyData.Select(
            (r, idx) => r.ToCalculated(
                periodMin,
                diff[idx],
                tiePointData.MarketDateHigh[idx],
                tiePointData.MarketDateLow[idx],
                tiePointData.TiePoint[idx],
                candleDirection[idx],
                emaDict[idx]
            )
        );
    }
}