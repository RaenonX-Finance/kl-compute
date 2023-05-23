using System.Diagnostics;
using System.Linq.Expressions;
using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

namespace KL.Common.Controllers;


public struct UpdateAllArgs {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required HistoryInterval Interval { get; init; }
}

public static class HistoryDataController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataController));

    private static readonly FilterDefinitionBuilder<HistoryDataModel> FilterBuilder =
        Builders<HistoryDataModel>.Filter;

    private static readonly Dictionary<UpdateAllArgs, DelayedOperation<IList<HistoryDataModel>>> BatchUpdateAll = new();

    public static IEnumerable<HistoryDataModel> GetAll(string symbol, HistoryInterval interval) {
        Log.Information("Request all history data of {Symbol} @ {Interval}", symbol, interval);

        return MongoConst.GetHistoryCollection(symbol)
            .AsQueryable()
            .Where(r => r.Interval == interval)
            .OrderBy(r => r.Timestamp);
    }

    public static IEnumerable<HistoryDataModel> GetLastN(string symbol, HistoryInterval interval, int limit) {
        Log.Information(
            "Request {limit} history data of {Symbol} @ {Interval}",
            limit,
            symbol,
            interval
        );

        return MongoConst.GetHistoryCollection(symbol)
            .AsQueryable()
            .Where(r => r.Interval == interval)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToEnumerable()
            .OrderBy(r => r.Timestamp);
    }

    public static IDictionary<string, IEnumerable<HistoryDataModel>> GetAtTime(
        IList<string> symbols,
        IList<DateTime> timestamps
    ) {
        if (timestamps.IsEmpty()) {
            Log.Information(
                "Request history data of {@Symbols} does not have timestamp specified, returning empty response",
                symbols
            );
            return symbols.ToDictionary(
                symbol => symbol,
                _ => Enumerable.Empty<HistoryDataModel>()
            );
        }

        Log.Information(
            "Request history data of {@Symbols} at {@Timestamps} ({TimestampCount})",
            symbols,
            timestamps,
            timestamps.Count
        );

        return symbols.ToDictionary(
            symbol => symbol,
            symbol => MongoConst.GetHistoryCollection(symbol)
                .AsQueryable()
                .Where(r => timestamps.Contains(r.Timestamp))
                .OrderByDescending(r => r.Timestamp)
                .ToEnumerable()
        );
    }

    public static IEnumerable<HistoryDataModel> GetBeforeTime(
        string symbol,
        HistoryInterval interval,
        DateTime maxTimeExclusive,
        int limit
    ) {
        Log.Information(
            "Request {limit} history data of {Symbol} @ {Interval} before {MaxTimeExclusive}",
            limit,
            symbol,
            interval,
            maxTimeExclusive
        );

        return MongoConst.GetHistoryCollection(symbol)
            .AsQueryable()
            .Where(r => r.Interval == interval && r.Timestamp < maxTimeExclusive)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToEnumerable()
            .OrderBy(r => r.Timestamp);
    }

    public static async Task<DateTimeRangeModel?> GetStoredDataRange(string symbol, HistoryInterval interval) {
        Log.Information("Requesting available data range of {Symbol} @ {Interval}", symbol, interval);

        Expression<Func<HistoryDataModel, bool>> filter = r => r.Interval == interval;
        Expression<Func<HistoryDataModel, object>> sort = r => r.Timestamp;

        try {
            var range = await Task.WhenAll(
                MongoConst.GetHistoryCollection(symbol)
                    .Find(filter)
                    .SortBy(sort)
                    .Limit(1)
                    .SingleAsync(),
                MongoConst.GetHistoryCollection(symbol)
                    .Find(filter)
                    .SortByDescending(sort)
                    .Limit(1)
                    .SingleAsync()
            );

            var start = range[0];
            var end = range[1];

            return new DateTimeRangeModel {
                Start = start.Timestamp,
                End = end.Timestamp
            };
        } catch (InvalidOperationException e) {
            if (e.Message == "Sequence contains no elements") {
                return null;
            }

            throw;
        }
    }

    public static void UpdateAllBatched(
        string symbol,
        HistoryInterval interval,
        IList<HistoryDataModel> entries
    ) {
        Log.Information(
            "Queued batch update {Count} history data of {Symbol} @ {Interval} from {Start} to {End}",
            entries.Count,
            symbol,
            interval,
            entries.Select(r => r.Timestamp).Min(),
            entries.Select(r => r.Timestamp).Max()
        );

        var args = new UpdateAllArgs {
            Symbol = symbol,
            Interval = interval
        };

        BatchUpdateAll.TryAdd(
            args,
            new DelayedOperation<IList<HistoryDataModel>>(
                data => UpdateAll(symbol, interval, data, 0),
                TimeSpan.FromMilliseconds(EnvironmentConfigHelper.Config.Source.Common.History.BatchUpdateDelayMs)
            )
        );
        BatchUpdateAll[args].UpdateArgs(entries);
    }

    public static Task UpdateAll(
        string symbol,
        HistoryInterval interval,
        IList<HistoryDataModel> entries,
        int maxWriteConflictRetryCount = 5
    ) {
        return UpdateAll(symbol, interval, entries, maxWriteConflictRetryCount, 0);
    }

    private static async Task UpdateAll(
        string symbol,
        HistoryInterval interval,
        IList<HistoryDataModel> entries,
        int maxWriteConflictRetryCount,
        int currentWriteConflictRetryCount
    ) {
        try {
            if (entries.IsEmpty()) {
                Log.Warning("{Symbol} @ {Interval} has nothing to update", symbol, interval);
                return;
            }

            var start = Stopwatch.GetTimestamp();
            using var session = await MongoSession.Create();
            var earliest = entries.Select(r => r.Timestamp).Min();
            var latest = entries.Select(r => r.Timestamp).Max();

            Log.Information(
                "Session {Session}: To update {Count} history data of {Symbol} @ {Interval} from {Start} to {End}",
                session.SessionId,
                entries.Count,
                symbol,
                interval,
                earliest,
                latest
            );

            var filter = FilterBuilder.Where(
                r =>
                    r.Interval == interval
                    && r.Timestamp >= earliest
                    && r.Timestamp <= latest
            );

            await MongoConst.GetHistoryCollection(symbol).DeleteManyAsync(session.Session, filter);
            await MongoConst.GetHistoryCollection(symbol).InsertManyAsync(session.Session, entries);

            await session.Session.CommitTransactionAsync();

            Log.Information(
                "Session {Session}: Updated {Count} history data of {Symbol} @ {Interval} from {Start} to {End} in {Elapsed:0.00} ms",
                session.SessionId,
                entries.Count,
                symbol,
                interval,
                earliest,
                latest,
                start.GetElapsedMs()
            );
        } catch (MongoCommandException ex) {
            if (currentWriteConflictRetryCount < maxWriteConflictRetryCount || !ex.IsWriteConflictError()) {
                throw;
            }

            Log.Warning(
                "Write conflict occurred during the update of history data of {Symbol} @ {Interval} ({Count}), will retry",
                symbol,
                interval,
                entries.Count
            );
            await UpdateAll(symbol, interval, entries, maxWriteConflictRetryCount, currentWriteConflictRetryCount + 1);
        }
    }
}