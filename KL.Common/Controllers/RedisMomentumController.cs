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
        await GetDatabase().StringSetAsync(KeyOfSingleData(symbol), (int)momentum);
    }

    public static async Task<Momentum> Get(string symbol) {
        var redisResult = await GetDatabase().StringGetAsync(KeyOfSingleData(symbol));
        if (!redisResult.TryParse(out int momentum)) {
            throw new InvalidDataException($"Invalid momentum stored for [${symbol}]");
        }

        return (Momentum)momentum;
    }
}