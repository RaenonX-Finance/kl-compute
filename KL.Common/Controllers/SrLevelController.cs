using System.Collections.Immutable;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;
using Serilog;

namespace KL.Common.Controllers;


public static class SrLevelController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SrLevelController));

    private static readonly FilterDefinitionBuilder<SrLevelDataModel>
        FilterBuilder = Builders<SrLevelDataModel>.Filter;

    public static async Task UpdateAll(IList<SrLevelDataModel> data) {
        if (data.Count == 0) {
            Log.Warning("No SR level data to update");
            return;
        }

        var symbols = data.Select(r => r.Symbol).Distinct().ToImmutableSortedSet();
        using var session = await MongoSession.Create();

        Log.Information(
            "Session {Session}: To update {Count} SR level data of {@Symbols}",
            session.SessionId,
            data.Count,
            symbols
        );

        await MongoConst.PxSrLevel.DeleteManyAsync(
            session.Session,
            FilterBuilder.Where(r => symbols.Contains(r.Symbol))
        );
        await MongoConst.PxSrLevel.InsertManyAsync(session.Session, data);

        await session.Session.CommitTransactionAsync();

        Log.Information(
            "Session {Session}: Updated {Count} SR level data of {@Symbols}",
            session.SessionId,
            data.Count,
            symbols
        );
    }
}