using System.Collections.Immutable;
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
        SrLevelType srLevelType,
        ProductCategory category
    ) {
        var dates = new List<DateTime>();

        // Adding 1 day to prevent losing the current day
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        var srLevelTiming = PxConfigController.Config.SrLevelTimingMap[category];
        var timing = srLevelTiming.GetTimingPair(srLevelType);

        if (timing == null) {
            return ImmutableArray<DateTime>.Empty;
        }

        while (dates.Count < minPairs * 2) {
            dates.AddRange(
                timing.Timings
                    .Select(
                        time => date.ToDateTime(time)
                            .ToTimezone(timing.Timezone)
                            .ToTimezone(TimeZoneInfo.Utc)
                    )
                    .Where(r => r <= DateTime.UtcNow)
            );

            date = date.AddBusinessDay(-1);
        }

        return dates.OrderDescending().ToImmutableArray();
    }

    private static IImmutableList<SrLevelDataModel> GetSrLevelModels(
        string symbol,
        IImmutableList<HistoryDataModel> dataOpenCloseCrossed,
        SrLevelTimingModel srLevelTiming,
        SrLevelType srLevelType,
        int pairCount
    ) {
        if (dataOpenCloseCrossed.Count == 0) {
            var e = new InvalidOperationException($"No data of {symbol} to calculate SR level");
            Log.Error(
                e,
                "No data of {Symbol} to calculate SR level of type {SrType}: {@Timing}",
                symbol,
                srLevelType,
                srLevelTiming
            );
            throw e;
        }

        var timing = srLevelTiming.GetTimingPair(srLevelType);

        if (timing == null) {
            return ImmutableArray<SrLevelDataModel>.Empty;
        }

        var srLevelModels = new List<SrLevelDataModel>();
        using var dataEnumerator = dataOpenCloseCrossed.OrderByDescending(r => r.Timestamp).GetEnumerator();
        dataEnumerator.MoveNext(); // Enumerator starts with `Current` being `null`
        var dataEnumeratorMoved = true;

        while (srLevelModels.Count < pairCount && dataEnumeratorMoved) {
            var currentOpen = dataEnumerator.Current;
            var currentOpenTime = TimeOnly.FromDateTime(currentOpen.Timestamp.ToTimezoneFromUtc(timing.Timezone));

            if (currentOpenTime != timing.Open) {
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
                    Type = srLevelType,
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
                "Data of {Symbol} given for calculating SR level is not enough ({DataCount}) to meet the desired pairs ({ModelCount})",
                symbol,
                dataOpenCloseCrossed.Count,
                pairCount
            );
        }

        return srLevelModels.ToImmutableArray();
    }

    private static IEnumerable<SrLevelDataModel> CalcLevelsByProductCategory(
        IList<string> symbols,
        ProductCategory category,
        int pairCount
    ) {
        var groupedData = Enum.GetValues(typeof(SrLevelType))
            .Cast<SrLevelType>()
            .SelectMany(
                srLevelType => HistoryDataController.GetAtTime(
                        symbols,
                        GetKeyTimestamps(pairCount + 1, srLevelType, category)
                    )
                    .GroupBy(r => r.Symbol)
                    .Select(
                        r => new {
                            Symbol = r.Key,
                            SrLevelType = srLevelType,
                            Data = r.ToImmutableArray()
                        }
                    )
            )
            .ToImmutableDictionary(
                r => new { r.Symbol, r.SrLevelType },
                r => r.Data
            );

        var models = new List<SrLevelDataModel>();

        foreach (var symbol in symbols) {
            var timing = PxConfigController.Config.SrLevelTimingMap[category];

            foreach (var srLevelType in Enum.GetValues(typeof(SrLevelType)).Cast<SrLevelType>()) {
                if (!groupedData.TryGetValue(new { Symbol = symbol, SrLevelType = srLevelType }, out var data)) {
                    // Data could be unavailable because timing is unavailable
                    // For example, `Secondary` of `NQ`
                    continue;
                }

                models.AddRange(GetSrLevelModels(symbol, data, timing, srLevelType, pairCount));
            }
        }

        return models;
    }

    public static IList<SrLevelDataModel> CalcLevels(IEnumerable<string> symbols) {
        return symbols
            .Select(symbol => PxConfigController.Config.Sources.First(source => source.InternalSymbol == symbol))
            .GroupBy(r => r.ProductCategory)
            .SelectMany(
                r => CalcLevelsByProductCategory(
                    r.Select(source => source.InternalSymbol).ToImmutableArray(),
                    r.Key,
                    PxConfigController.Config.SrLevel.PairCount
                )
            )
            .ToImmutableArray();
    }
}