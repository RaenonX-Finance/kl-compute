using System.Collections.Immutable;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.Common.Utils;
using Serilog;
using StackExchange.Redis;

namespace KL.Common.Controllers;


public static class RedisLastPxController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RedisLastPxController));
    
    private const string SourcesInUseKey = "Sources";

    public static async Task Initialize() {
        await RedisHelper.ClearDb(RedisDbId.LastPx);
    }

    private static string KeyOfSingleData(string symbol, long epochSec) {
        return $"{symbol}:{epochSec}";
    }

    private static IDatabase GetDatabase() {
        return RedisHelper.GetDb(RedisDbId.LastPx);
    }

    private static async Task UpdateEpochSec(
        IDatabaseAsync db,
        string symbol,
        IImmutableList<IHistoryDataEntry> entries
    ) {
        var epochSecEntries = entries
            .Select(
                r => {
                    var epochSec = r.Timestamp.ToEpochSeconds();

                    return new SortedSetEntry(epochSec, epochSec);
                }
            )
            .ToArray();

        await db.SortedSetAddAsync(symbol, epochSecEntries);
    }

    private static async Task UpdatePx(
        IDatabaseAsync db,
        string symbol,
        IImmutableList<IHistoryDataEntry> entries
    ) {
        var lastPxValues = entries.Select(
                r => new KeyValuePair<RedisKey, RedisValue>(KeyOfSingleData(symbol, r.Timestamp.ToEpochSeconds()), (double)r.Close)
            )
            .ToArray();

        await db.StringSetAsync(lastPxValues);
    }

    public static async Task Set(string symbol, IImmutableList<IHistoryDataEntry> entries, bool isCreate = false) {
        var db = GetDatabase();

        if (isCreate) {
            await db.KeyDeleteAsync(symbol);
        }

        await Task.WhenAll(
            UpdatePx(db, symbol, entries),
            UpdateEpochSec(db, symbol, entries),
            db.SetAddAsync(SourcesInUseKey, symbol)
        );
    }

    private static async Task CreateNewBarOfSymbol(IDatabaseAsync db, string symbol, DateTime timestamp) {
        var epochSecToRemove = await db.SortedSetPopAsync(symbol);

        if (epochSecToRemove == null) {
            Log.Warning("{Symbol} does not have px data, failed to create new bar", symbol);
            return;
        }

        var keyToRemove = KeyOfSingleData(symbol, (long)epochSecToRemove.Value.Score);

        (await db.StringGetAsync(keyToRemove)).TryParse(out double lastClose);
        await db.KeyDeleteAsync(keyToRemove);
        await db.StringSetAsync(KeyOfSingleData(symbol, timestamp.ToEpochSeconds()), lastClose);
    }
    
    public static async Task<IEnumerable<string>> CreateNewBar(DateTime timestamp) {
        var db = GetDatabase();

        var symbolsToCreate = db.SetMembers(SourcesInUseKey).Select(r => r.ToString()).ToImmutableArray();

        await Task.WhenAll(symbolsToCreate.Select(r => CreateNewBarOfSymbol(db, r, timestamp)));

        return symbolsToCreate;
    }
}