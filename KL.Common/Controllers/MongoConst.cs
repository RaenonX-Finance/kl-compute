using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Utils;
using MongoDB.Driver;

namespace KL.Common.Controllers;


public static class MongoConst {
    public static readonly IMongoClient Client
        = new MongoClient(EnvironmentConfigHelper.Config.Database.MongoUrl).Initialize();

    private static readonly IMongoDatabase PxDatabase = Client.GetDatabase("px");

    public static readonly IMongoDatabase PxHistDatabase = Client.GetDatabase("pxHist");

    public static readonly IMongoDatabase PxCalcDatabase = Client.GetDatabase("pxCalc");

    public static readonly IMongoCollection<SrLevelDataModel> PxSrLevel =
        PxDatabase.GetCollection<SrLevelDataModel>("srLevel");

    public static readonly IMongoCollection<SourceInfoModel> PxSourceInfo =
        PxDatabase.GetCollection<SourceInfoModel>("info");

    public static readonly IMongoCollection<PxConfigModel> PxCalcConfig =
        PxDatabase.GetCollection<PxConfigModel>("config");

    public static IMongoCollection<HistoryDataModel> GetHistoryCollection(string symbol) {
        return PxHistDatabase.GetCollection<HistoryDataModel>(symbol);
    }
    
    public static IMongoCollection<CalculatedDataModel> GetCalculatedCollection(string symbol) {
        return PxCalcDatabase.GetCollection<CalculatedDataModel>(symbol);
    }
}