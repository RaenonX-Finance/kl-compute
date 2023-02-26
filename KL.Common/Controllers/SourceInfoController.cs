using System.Collections.Immutable;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;
using Serilog;

namespace KL.Common.Controllers;


public static class SourceInfoController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SourceInfoController));

    private static readonly FilterDefinitionBuilder<SourceInfoModel> FilterBuilder =
        Builders<SourceInfoModel>.Filter;

    public static async Task UpdateAll(IImmutableList<SourceInfoModel> sourceInfo) {
        var symbols = sourceInfo.Select(r => r.Symbol).ToImmutableArray();

        using var session = await MongoSession.Create();

        Log.Information(
            "Session {Session}: To update {Count} source info ({@Symbols})",
            session.SessionId,
            sourceInfo.Count,
            symbols
        );

        var filter = FilterBuilder.Where(r => symbols.Contains(r.Symbol));

        await MongoConst.PxSourceInfo.DeleteManyAsync(session.Session, filter);
        await MongoConst.PxSourceInfo.InsertManyAsync(session.Session, sourceInfo);

        await session.Session.CommitTransactionAsync();

        Log.Information(
            "Session {Session}: Updated {Count} source info ({@Symbols})",
            session.SessionId,
            sourceInfo.Count,
            symbols
        );
    }
}