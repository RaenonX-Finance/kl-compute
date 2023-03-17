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

        return MongoConst.PxCalculated.AsQueryable()
            .Where(r => r.Symbol == symbol && r.PeriodMin == periodMin)
            .OrderByDescending(r => r.EpochSecond)
            .Take(limit)
            // `IMongoQueryable` does not support `.OrderByDescending()` with `.Reverse()`
            .ToArray()
            .Reverse();
    }

    public static async Task UpdateByEpoch(IList<CalculatedDataModel> calculatedData) {
        using var session = await MongoSession.Create();

        Log.Debug(
            "Session {Session}: To update {Count} calculated data",
            session.SessionId,
            calculatedData.Count
        );

        var filter = calculatedData
            .Select(r => (r.Symbol, r.PeriodMin, r.EpochSecond))
            .Distinct()
            .Select(
                pair => FilterBuilder.Where(
                    r =>
                        r.Symbol == pair.Symbol
                        && r.PeriodMin == pair.PeriodMin
                        && r.EpochSecond == pair.EpochSecond
                )
            );
        await MongoConst.PxCalculated.DeleteManyAsync(session.Session, FilterBuilder.Or(filter));
        await MongoConst.PxCalculated.InsertManyAsync(session.Session, calculatedData);

        await session.Session.CommitTransactionAsync();

        Log.Debug(
            "Session {Session}: Updated {Count} calculated data",
            session.SessionId,
            calculatedData.Count
        );
    }

    public static async Task UpdateByEpoch(CalculatedDataModel calculatedData) {
        Log.Debug("To update calculated data of {Symbol} at {DataTime}", calculatedData.Symbol, calculatedData.Date);

        await MongoConst.PxCalculated.ReplaceOneAsync(
            r => r.Symbol == calculatedData.Symbol
                 && r.PeriodMin == calculatedData.PeriodMin
                 && r.EpochSecond == calculatedData.EpochSecond,
            calculatedData
        );

        Log.Debug("Updated calculated data of {Symbol} at {DataTime}", calculatedData.Symbol, calculatedData.Date);
    }

    public static async Task AddData(MongoSession session, IEnumerable<CalculatedDataModel> calculatedData) {
        var start = Stopwatch.GetTimestamp();

        Log.Information("Session {Session}: To add calculated data", session.SessionId);

        await MongoConst.PxCalculated.InsertManyAsync(session.Session, calculatedData);

        Log.Information(
            "Session {Session}: Added calculated data in {Elapsed:0.00} ms",
            session.SessionId,
            start.GetElapsedMs()
        );
    }

    public static async Task RemoveData(MongoSession session, IList<(string Symbol, int PeriodMin)> symbolPeriodPair) {
        var start = Stopwatch.GetTimestamp();

        Log.Information(
            "Session {Session}: To remove calculated data of {@SymbolPeriodPair}",
            session.SessionId,
            symbolPeriodPair
        );

        var filter = symbolPeriodPair
            .Distinct()
            .Select(pair => FilterBuilder.Where(r => r.Symbol == pair.Symbol && r.PeriodMin == pair.PeriodMin));
        await MongoConst.PxCalculated.DeleteManyAsync(session.Session, FilterBuilder.Or(filter));

        Log.Information(
            "Session {Session}: Removed calculated data of {SymbolPeriodPair} in {Elapsed:0.00} ms",
            session.SessionId,
            symbolPeriodPair,
            start.GetElapsedMs()
        );
    }
}