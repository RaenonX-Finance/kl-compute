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

    private static Task<string> HistoryLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "HistoryLookup"
        };
        var indexKeys = Builders<HistoryDataModel>.IndexKeys
            .Ascending(data => data.Symbol)
            .Ascending(data => data.Interval)
            .Descending(data => data.Timestamp);
        var indexModel = new CreateIndexModel<HistoryDataModel>(indexKeys, indexOptions);

        return MongoConst.PxHistory.Indexes.CreateOneAsync(indexModel);
    }

    private static Task<string> CalculatedLookup() {
        var indexOptions = new CreateIndexOptions {
            Unique = true,
            Name = "CalculatedLookup"
        };
        var indexKeys = Builders<CalculatedDataModel>.IndexKeys
            .Ascending(data => data.Symbol)
            .Ascending(data => data.PeriodMin)
            .Descending(data => data.EpochSecond);
        var indexModel = new CreateIndexModel<CalculatedDataModel>(indexKeys, indexOptions);

        return MongoConst.PxCalculated.Indexes.CreateOneAsync(indexModel);
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