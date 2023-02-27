using KL.Common.Enums;
using KL.Common.Utils;
using StackExchange.Redis;

namespace KL.Common.Controllers;


public static class RedisMomentumController {
    private static IDatabase GetDatabase() {
        return RedisHelper.GetDb(RedisDbId.LastPxAndMomentum);
    }

    private static string KeyOfSingleData(string symbol) {
        return $"{symbol}:Momentum";
    }

    public static async Task Set(string symbol, Momentum momentum) {
        var db = GetDatabase();
        await db.StringSetAsync(KeyOfSingleData(symbol), (int)momentum);
    }
}