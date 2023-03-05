using KL.Common.Utils;
using MongoDB.Driver;
using Serilog;

namespace KL.Common.Controllers;


public static class MongoManager {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(MongoManager));

    public static async Task Initialize() {
        MongoConst.Client.Ping();

        await Task.WhenAll(MongoIndexManager.Initialize());
    }

    private static void Ping(this IMongoClient client) {
        try {
            Log.Information(
                "Testing connection to MongoDB at {MongoAddress}",
                EnvironmentConfigHelper.Config.Database.MongoUrl
            );
            client.ListDatabaseNames().MoveNext();
        } catch (TimeoutException e) {
            Log.Error(
                e,
                "Error connecting to MongoDB at {MongoAddress}",
                EnvironmentConfigHelper.Config.Database.MongoUrl
            );
            Environment.Exit(1);
            throw;
        }
    }
}