using KL.Common.Controllers;
using KL.Common.Enums;
using Serilog;
using StackExchange.Redis;

namespace KL.Common.Utils;


public static class RedisHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RedisHelper));

    public static IDatabase GetDb(RedisDbId id) {
        return RedisConst.Redis.GetDatabase((int)id);
    }

    public static async Task TestConnection() {
        try {
            Log.Information(
                "Testing connection to Redis at {RedisAddress}",
                EnvironmentConfigHelper.Config.Database.RedisAddress
            );
            await RedisConst.Redis.GetDatabase().PingAsync();
        } catch (RedisConnectionException e) {
            Log.Error(
                e,
                "Error connecting to Redis at {RedisAddress}",
                EnvironmentConfigHelper.Config.Database.RedisAddress
            );
            Environment.Exit(1);
            throw;
        }
    }

    public static async Task ClearDb(RedisDbId id) {
        await Task.WhenAll(RedisConst.Redis.GetServers().Select(server => server.FlushDatabaseAsync((int)id)));
    }
}