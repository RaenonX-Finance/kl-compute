using System.Diagnostics;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Serilog;

namespace KL.Common.Controllers;


public static class CalculatedDataController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(CalculatedDataController));

    private static readonly FilterDefinitionBuilder<CalculatedDataModel> FilterBuilder =
        Builders<CalculatedDataModel>.Filter;

    public static IEnumerable<CalculatedDataModel> GetData(string symbol, int periodMin, int limit) {
        Log.Debug(
            "Get {Limit} calculated data of {Symbol} @ {PeriodMin}",
            limit,
            symbol,
            periodMin
        );

        return MongoConst.GetCalculatedCollection(symbol)
            .AsQueryable()
            .Where(r => r.PeriodMin == periodMin)
            .OrderByDescending(r => r.EpochSecond)
            .Take(limit)
            // `IMongoQueryable` does not support `.OrderByDescending()` with `.Reverse()`
            .ToArray()
            .Reverse();
    }

    public static async Task UpdateByEpoch(string symbol, IList<CalculatedDataModel> calculatedData) {
        try {
            using var session = await MongoSession.Create();

            Log.Debug(
                "Session {Session}: To update {Count} calculated data",
                session.SessionId,
                calculatedData.Count
            );

            var filter = calculatedData
                .Select(r => (r.PeriodMin, r.EpochSecond))
                .Distinct()
                .Select(
                    pair => FilterBuilder.Where(
                        r =>
                            r.PeriodMin == pair.PeriodMin
                            && r.EpochSecond == pair.EpochSecond
                    )
                );
            await MongoConst.GetCalculatedCollection(symbol).DeleteManyAsync(session.Session, FilterBuilder.Or(filter));
            await MongoConst.GetCalculatedCollection(symbol).InsertManyAsync(session.Session, calculatedData);

            await session.Session.CommitTransactionAsync();

            Log.Debug(
                "Session {Session}: Updated {Count} calculated data",
                session.SessionId,
                calculatedData.Count
            );
        } catch (MongoCommandException ex) {
            if (!ex.IsWriteConflictError()) {
                throw;
            }

            Log.Warning(
                "Write conflict occurred during the update of calculated data of {Symbol} ({Count}), will retry",
                symbol,
                calculatedData.Count
            );
            await UpdateByEpoch(symbol, calculatedData);
        }
    }

    public static async Task UpdateByEpoch(string symbol, CalculatedDataModel calculatedData) {
        Log.Debug("To update calculated data of {Symbol} at {DataTime}", symbol, calculatedData.Date);

        await MongoConst.GetCalculatedCollection(symbol)
            .ReplaceOneAsync(
                r => r.PeriodMin == calculatedData.PeriodMin
                     && r.EpochSecond == calculatedData.EpochSecond,
                calculatedData
            );

        Log.Debug("Updated calculated data of {Symbol} at {DataTime}", symbol, calculatedData.Date);
    }

    public static async Task AddData(string symbol, IEnumerable<CalculatedDataModel> calculatedData) {
        var start = Stopwatch.GetTimestamp();

        Log.Information("To add calculated data of {Symbol}", symbol);

        await MongoConst.GetCalculatedCollection(symbol).InsertManyAsync(calculatedData);

        Log.Information(
            "Added calculated data of {Symbol} in {Elapsed:0.00} ms",
            symbol,
            start.GetElapsedMs()
        );
    }

    public static async Task RemoveData((string Symbol, int PeriodMin) c) {
        var start = Stopwatch.GetTimestamp();

        Log.Information("To remove calculated data of {@SymbolPeriodPair}", c);

        await MongoConst
            .GetCalculatedCollection(c.Symbol)
            .DeleteManyAsync(r => r.PeriodMin == c.PeriodMin);

        Log.Information("Removed calculated data of {@SymbolPeriodPair} in {Elapsed:0.00} ms", c, start.GetElapsedMs());
    }
}