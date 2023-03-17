using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Models.Config;
using ILogger = Serilog.ILogger;

namespace KL.Calc.Computer;


public static class SrLevelComputer {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SrLevelComputer));

    private static IList<DateTime> GetKeyTimestamps(
        int minPairs,
        ProductCategory category
    ) {
        var dates = new List<DateTime>();

        // Adding 1 day to prevent losing the current day
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var srLevelTiming = PxConfigController.Config.SrLevelTimingMap[category];

        while (dates.Count < minPairs * 2) {
            dates.AddRange(
                srLevelTiming.Timings
                    .Select(
                        time => date
                            .ToDateTime(time, DateTimeKind.Unspecified)
                            .FromTimezoneToUtc(srLevelTiming.Timezone)
                    )
                    .Where(r => r <= DateTime.UtcNow)
            );

            date = date.AddBusinessDay(-1);
        }

        return dates.OrderDescending().ToArray();
    }

    private static IEnumerable<SrLevelDataModel> GetSrLevelModels(
        string symbol,
        ICollection<HistoryDataModel> dataOpenCloseCrossed,
        SrLevelTimingModel srLevelTiming,
        int pairCount
    ) {
        if (dataOpenCloseCrossed.IsEmpty()) {
            var e = new InvalidOperationException($"No data of {symbol} to calculate SR level");
            Log.Error(
                e,
                "No data of {Symbol} to calculate SR level using timing of {@Timing}",
                symbol,
                srLevelTiming
            );
            throw e;
        }

        var srLevelModels = new List<SrLevelDataModel>();
        using var dataEnumerator = dataOpenCloseCrossed.OrderByDescending(r => r.Timestamp).GetEnumerator();
        dataEnumerator.MoveNext(); // Enumerator starts with `Current` being `null`
        var dataEnumeratorMoved = true;

        while (srLevelModels.Count < pairCount && dataEnumeratorMoved) {
            var currentOpen = dataEnumerator.Current;
            var currentOpenTime
                = TimeOnly.FromDateTime(currentOpen.Timestamp.ToTimezoneFromUtc(srLevelTiming.Timezone));

            if (currentOpenTime != srLevelTiming.Open) {
                dataEnumeratorMoved = dataEnumerator.MoveNext();
                if (!dataEnumeratorMoved) {
                    break;
                }

                continue;
            }

            dataEnumeratorMoved = dataEnumerator.MoveNext();
            if (!dataEnumeratorMoved) {
                break;
            }

            var lastClose = dataEnumerator.Current;

            srLevelModels.Add(
                new SrLevelDataModel {
                    Symbol = symbol,
                    LastDate = DateOnly.FromDateTime(lastClose.Timestamp.ToTimezone(TimeZoneInfo.Utc)),
                    LastClose = lastClose.Close,
                    CurrentDate = DateOnly.FromDateTime(currentOpen.Timestamp.ToTimezone(TimeZoneInfo.Utc)),
                    CurrentOpen = currentOpen.Open
                }
            );

            dataEnumeratorMoved = dataEnumerator.MoveNext();
            if (!dataEnumeratorMoved) {
                break;
            }
        }

        if (srLevelModels.Count < pairCount) {
            Log.Warning(
                "Data of {Symbol} given for calculating SR level is not enough ({DataCount}) "
                + "to meet the desired pairs ({ActualModelCount} / {ExpectedModelCount})",
                symbol,
                dataOpenCloseCrossed.Count,
                srLevelModels.Count,
                pairCount
            );
        }

        return srLevelModels.ToArray();
    }

    private static IEnumerable<SrLevelDataModel> CalcLevelsByProductCategory(
        IList<string> symbols,
        ProductCategory category,
        int pairCount
    ) {
        var groupedData = HistoryDataController.GetAtTime(
                symbols,
                GetKeyTimestamps(pairCount + 1, category)
            )
            .GroupBy(r => r.Symbol)
            .ToDictionary(
                r => r.Key,
                r => r.ToArray()
            );

        var models = new List<SrLevelDataModel>();

        foreach (var symbol in symbols) {
            var timing = PxConfigController.Config.SrLevelTimingMap[category];

            models.AddRange(GetSrLevelModels(symbol, groupedData[symbol], timing, pairCount));
        }

        return models;
    }

    public static IList<SrLevelDataModel> CalcLevels(IEnumerable<string> symbols) {
        return symbols
            .Select(symbol => PxConfigController.Config.Sources[symbol])
            .GroupBy(r => r.ProductCategory)
            .SelectMany(
                r => CalcLevelsByProductCategory(
                    r.Select(source => source.InternalSymbol).ToArray(),
                    r.Key,
                    PxConfigController.Config.SrLevel.PairCount
                )
            )
            .ToArray();
    }
}