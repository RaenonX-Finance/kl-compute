using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;

namespace KL.Common.Controllers;


public static class MongoConst {
    public static readonly IMongoClient Client = new MongoClient(EnvironmentConfigHelper.Config.Database.MongoUrl);

    private static readonly IMongoDatabase PxDatabase = Client.GetDatabase("px");

    public static readonly IMongoCollection<HistoryDataModel> PxHistory =
        PxDatabase.GetCollection<HistoryDataModel>("dataNew");

    public static readonly IMongoCollection<CalculatedDataModel> PxCalculated =
        PxDatabase.GetCollection<CalculatedDataModel>("calcNew");

    public static readonly IMongoCollection<SrLevelDataModel> PxSrLevel =
        PxDatabase.GetCollection<SrLevelDataModel>("srLevel");

    public static readonly IMongoCollection<PxConfigModel> CalcConfig =
        PxDatabase.GetCollection<PxConfigModel>("config");
}