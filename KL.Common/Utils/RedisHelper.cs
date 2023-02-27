using KL.Common.Controllers;
using KL.Common.Enums;
using StackExchange.Redis;

namespace KL.Common.Utils;


public static class RedisHelper {
    public static IDatabase GetDb(RedisDbId id) {
        return RedisConst.Redis.GetDatabase((int)id);
    }

    public static async Task ClearDb(RedisDbId id) {
        await Task.WhenAll(RedisConst.Redis.GetServers().Select(server => server.FlushDatabaseAsync((int)id)));
    }
}