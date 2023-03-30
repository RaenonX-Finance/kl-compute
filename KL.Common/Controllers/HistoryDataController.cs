using System.Linq.Expressions;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

namespace KL.Common.Controllers;


public static class HistoryDataController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataController));

    private static readonly FilterDefinitionBuilder<HistoryDataModel> FilterBuilder =
        Builders<HistoryDataModel>.Filter;

    public static IEnumerable<HistoryDataModel> GetAll(string symbol, HistoryInterval interval) {
        Log.Information("Request all history data of {Symbol} @ {Interval}", symbol, interval);

        return MongoConst.PxHistory.AsQueryable()
            .Where(r => r.Symbol == symbol && r.Interval == interval)
            .OrderBy(r => r.Timestamp);
    }

    public static IEnumerable<HistoryDataModel> GetLastN(string symbol, HistoryInterval interval, int limit) {
        Log.Information(
            "Request {limit} history data of {Symbol} @ {Interval}",
            limit,
            symbol,
            interval
        );

        return MongoConst.PxHistory.AsQueryable()
            .Where(r => r.Symbol == symbol && r.Interval == interval)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToEnumerable()
            .OrderBy(r => r.Timestamp);
    }

    public static IEnumerable<HistoryDataModel> GetAtTime(
        IList<string> symbols,
        IList<DateTime> timestamps
    ) {
        if (timestamps.IsEmpty()) {
            Log.Information(
                "Request history data of {@Symbols} does not have timestamp specified, returning empty response",
                symbols
            );
            return Array.Empty<HistoryDataModel>();
        }

        Log.Information(
            "Request history data of {@Symbols} at {@Timestamps} ({TimestampCount})",
            symbols,
            timestamps,
            timestamps.Count
        );

        return MongoConst.PxHistory.AsQueryable()
            .Where(r => symbols.Contains(r.Symbol) && timestamps.Contains(r.Timestamp))
            .OrderByDescending(r => r.Timestamp);
    }

    public static async Task<DateTimeRangeModel?> GetStoredDataRange(string symbol, HistoryInterval interval) {
        Log.Information("Requesting available data range of {Symbol} @ {Interval}", symbol, interval);

        Expression<Func<HistoryDataModel, bool>> filter = r => r.Symbol == symbol && r.Interval == interval;
        Expression<Func<HistoryDataModel, object>> sort = r => r.Timestamp;

        try { 
            var range = await Task.WhenAll(
                MongoConst.PxHistory
                    .Find(filter)
                    .SortBy(sort)
                    .Limit(1)
                    .SingleAsync(),
                MongoConst.PxHistory
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

    public static async Task UpdateAll(
        string symbol,
        HistoryInterval interval,
        IList<HistoryDataModel> entries
    ) {
        if (entries.IsEmpty()) {
            Log.Warning("{Symbol} @ {Interval} has nothing to update", symbol, interval);
            return;
        }

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
                r.Symbol == symbol
                && r.Interval == interval
                && r.Timestamp >= earliest
                && r.Timestamp <= latest
        );

        await MongoConst.PxHistory.DeleteManyAsync(session.Session, filter);
        await MongoConst.PxHistory.InsertManyAsync(session.Session, entries);

        await session.Session.CommitTransactionAsync();

        Log.Information(
            "Session {Session}: Updated {Count} history data of {Symbol} @ {Interval} from {Start} to {End}",
            session.SessionId,
            entries.Count,
            symbol,
            interval,
            earliest,
            latest
        );
    }
}