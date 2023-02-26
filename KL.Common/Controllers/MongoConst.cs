using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;

namespace KL.Common.Controllers;


public static class MongoConst {
    public static readonly IMongoClient Client
        = new MongoClient(EnvironmentConfigHelper.Config.Database.MongoUrl).Initialize();

    private static readonly IMongoDatabase PxDatabase = Client.GetDatabase("px");

    public static readonly IMongoCollection<HistoryDataModel> PxHistory =
        PxDatabase.GetCollection<HistoryDataModel>("data");

    public static readonly IMongoCollection<CalculatedDataModel> PxCalculated =
        PxDatabase.GetCollection<CalculatedDataModel>("calc");

    public static readonly IMongoCollection<SrLevelDataModel> PxSrLevel =
        PxDatabase.GetCollection<SrLevelDataModel>("srLevel");

    public static readonly IMongoCollection<PxConfigModel> CalcConfig =
        PxDatabase.GetCollection<PxConfigModel>("config");
}