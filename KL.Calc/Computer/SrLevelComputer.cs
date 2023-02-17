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

    private static IImmutableList<DateTime> GetKeyTimestamps(int minPairs, ProductCategory category) {
        var dates = new List<DateTime>();

        // Adding 1 day to prevent losing the current day
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        while (dates.Count < minPairs * 2) {
            dates.AddRange(
                PxConfigController.Config.SrLevelTimingMap[category]
                    .Primary.Timings
                    .Select(
                        timing => date.ToDateTime(timing.Time)
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
        IImmutableList<HistoryDataModel> data,
        SrLevelTimingModel srLevelTiming,
        SrLevelType srLevelType,
        int pairCount
    ) {
        if (data.Count == 0) {
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

        var timing = srLevelType switch {
            SrLevelType.Primary => srLevelTiming.Primary,
            SrLevelType.Secondary => srLevelTiming.Secondary,
            _ => throw new ArgumentOutOfRangeException(
                nameof(srLevelType),
                srLevelType,
                "Given SR level type does not have corresponding timing config to use"
            )
        };

        if (timing == null) {
            return ImmutableArray<SrLevelDataModel>.Empty;
        }

        var srLevelModels = new List<SrLevelDataModel>();
        using var dataEnumerator = data.OrderByDescending(r => r.Timestamp).GetEnumerator();
        dataEnumerator.MoveNext(); // Enumerator starts with `Current` being `null`
        var dataEnumeratorMoved = true;

        while (srLevelModels.Count < pairCount && dataEnumeratorMoved) {
            var currentOpen = dataEnumerator.Current;
            var currentOpenTime = TimeOnly.FromDateTime(currentOpen.Timestamp.ToTimezoneFromUtc(timing.Open.Timezone));

            if (currentOpenTime != timing.Open.Time) {
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
                "Data given for calculating SR level is not enough ({DataCount}) to meet the desired pairs ({ModelCount})",
                data.Count,
                pairCount
            );
        }

        return srLevelModels.ToImmutableArray();
    }

    private static IEnumerable<SrLevelDataModel> CalcLevelsByProductCategory(
        IImmutableList<string> symbols,
        ProductCategory category,
        int pairCount
    ) {
        // Adding 1 because the last pair could be incomplete
        var groupedData = HistoryDataController.GetAtTime(symbols, GetKeyTimestamps(pairCount + 1, category))
            .GroupBy(r => r.Symbol)
            .ToDictionary(r => r.Key, r => r.ToImmutableArray());

        var models = new List<SrLevelDataModel>();

        foreach (var symbol in symbols) {
            var data = groupedData[symbol];
            var timing = PxConfigController.Config.SrLevelTimingMap[category];

            models.AddRange(GetSrLevelModels(symbol, data, timing, SrLevelType.Primary, pairCount));
            models.AddRange(GetSrLevelModels(symbol, data, timing, SrLevelType.Secondary, pairCount));
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