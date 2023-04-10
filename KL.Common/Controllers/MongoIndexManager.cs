using KL.Common.Extensions;
using KL.Common.Models;
using MongoDB.Driver;

namespace KL.Common.Controllers;


public static class MongoIndexManager {
    public static IEnumerable<Task> Initialize() {
        return new List<Task> {
            HistoryLookup(),
            CalculatedLookup(),
            SrLevelLookup(),
            SourceInfoLookup()
        };
    }

    private static async Task HistoryLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "HistoryLookup"
        };
        var indexKeys = Builders<HistoryDataModel>.IndexKeys
            .Ascending(data => data.Interval)
            .Descending(data => data.Timestamp);
        var indexModel = new CreateIndexModel<HistoryDataModel>(indexKeys, indexOptions);

        await Task.WhenAll(
            (await MongoConst.PxHistDatabase.GetCollectionNames())
            .Select(symbol => MongoConst.GetHistoryCollection(symbol).Indexes.CreateOneAsync(indexModel))
        );
    }

    private static async Task CalculatedLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "CalculatedLookup"
        };
        var indexKeys = Builders<CalculatedDataModel>.IndexKeys
            .Ascending(data => data.PeriodMin)
            .Descending(data => data.EpochSecond);
        var indexModel = new CreateIndexModel<CalculatedDataModel>(indexKeys, indexOptions);

        await Task.WhenAll(
            (await MongoConst.PxCalcDatabase.GetCollectionNames())
            .Select(symbol => MongoConst.GetCalculatedCollection(symbol).Indexes.CreateOneAsync(indexModel))
        );
    }

    private static Task<string> SrLevelLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "SrLevelLookup"
        };
        var indexKeys = Builders<SrLevelDataModel>.IndexKeys
            .Ascending(data => data.Symbol)
            .Ascending(data => data.CurrentDate);
        var indexModel = new CreateIndexModel<SrLevelDataModel>(indexKeys, indexOptions);

        return MongoConst.PxSrLevel.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> SourceInfoLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "SourceInfoLookup"
        };
        var indexKeys = Builders<SourceInfoModel>.IndexKeys
            .Ascending(data => data.Symbol);
        var indexModel = new CreateIndexModel<SourceInfoModel>(indexKeys, indexOptions);

        return MongoConst.PxSourceInfo.Indexes.CreateOneAsync(indexModel);
    }
}