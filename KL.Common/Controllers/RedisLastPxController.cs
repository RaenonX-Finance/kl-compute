using System.Collections.Immutable;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.Common.Utils;
using StackExchange.Redis;

namespace KL.Common.Controllers;


internal record PxAtEpochMeta {
    public required string Key { get; init; }

    public required long EpochSec { get; init; }
}

internal static class PxCloseDataExtension {
    public static PxAtEpochMeta ToPxAtEpochMeta(this SortedSetEntry? entry, string symbol) {
        if (entry is null) {
            throw new InvalidDataException($"{symbol} does not have px data, failed to create new bar");
        }

        var epochSec = (long)entry.Value.Score;
        var key = RedisLastPxController.KeyOfPxAtEpoch(symbol, epochSec);

        return new PxAtEpochMeta { Key = key, EpochSec = epochSec };
    }
}

public static class RedisLastPxController {
    private const string SourcesInUseKey = "Sources";

    private const string LastMetaMinName = "Min";
    private const string LastMetaMaxName = "Max";
    private const string LastMetaEpochMsName = "EpochMs";
    private const string LastMetaUpdatedName = "Updated";

    public static async Task Initialize() {
        await RedisHelper.ClearDb(RedisDbId.LastPxAndMomentum);
    }

    public static string KeyOfPxAtEpoch(string symbol, long epochSec) {
        return $"{symbol}:{epochSec}";
    }

    private static string KeyOfLastMeta(string symbol) {
        return $"LastMeta:{symbol}";
    }

    private static IDatabase GetDatabase() {
        return RedisHelper.GetDb(RedisDbId.LastPxAndMomentum);
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
                r => new KeyValuePair<RedisKey, RedisValue>(
                    KeyOfPxAtEpoch(symbol, r.Timestamp.ToEpochSeconds()),
                    (double)r.Close
                )
            )
            .ToArray();

        await db.StringSetAsync(lastPxValues);
    }

    private static async Task CreateLastMeta(IDatabaseAsync db, string symbol, double px) {
        // Overwrites `lastMeta` - this is expected to call for every minute change
        await db.HashSetAsync(
            KeyOfLastMeta(symbol),
            new[] {
                new HashEntry(LastMetaMinName, px),
                new HashEntry(LastMetaMaxName, px),
                new HashEntry(LastMetaEpochMsName, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new HashEntry(LastMetaUpdatedName, true)
            }
        );
    }

    private static async Task UpdateLastMeta(IDatabaseAsync db, string symbol, double px) {
        var key = KeyOfLastMeta(symbol);
        var meta = (await db.HashGetAllAsync(key))
            .ToDictionary(r => r.Name, r => r.Value);

        if (meta.Count == 0) {
            await CreateLastMeta(db, symbol, px);
            return;
        }

        var update = false;
        var nowEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var redisEntries = new List<HashEntry>();

        // Using `else if` because this only sets `updated`
        // If the 1st condition matches, the result of 2nd one doesn't matter
        // Could use `||`, but it's harder to read
        if (meta[LastMetaUpdatedName].TryParse(out int updatedInt) && Convert.ToBoolean(updatedInt)) {
            update = true;
        } else if (
            meta[LastMetaEpochMsName].TryParse(out long epochMs)
            && nowEpochMs - epochMs > PxConfigController.Config.Cache.MarketUpdateGapMs
        ) {
            update = true;
        }

        if (meta[LastMetaMinName].TryParse(out double min) && px < min) {
            update = true;
            redisEntries.Add(new HashEntry(LastMetaMinName, Math.Min(px, min)));
        }

        if (meta[LastMetaMaxName].TryParse(out double max) && px > max) {
            update = true;
            redisEntries.Add(new HashEntry(LastMetaMaxName, Math.Max(px, max)));
        }

        if (update) {
            // Only update epoch entry if updated
            redisEntries.Add(new HashEntry(LastMetaEpochMsName, nowEpochMs));
            redisEntries.Add(new HashEntry(LastMetaUpdatedName, true));
        }

        await db.HashSetAsync(key, redisEntries.ToArray());
    }

    public static async Task<bool> PopUpdated(string symbol) {
        var db = GetDatabase();
        var key = KeyOfLastMeta(symbol);

        (await db.HashGetAsync(key, LastMetaUpdatedName)).TryParse(out int updatedInt);

        var updated = Convert.ToBoolean(updatedInt);

        await db.HashSetAsync(key, LastMetaUpdatedName, false);

        return updated;
    }

    public static async Task Set(string symbol, IImmutableList<IHistoryDataEntry> entries, bool isCreate = false) {
        var db = GetDatabase();

        if (isCreate) {
            await db.KeyDeleteAsync(symbol);
        }

        await Task.WhenAll(
            UpdatePx(db, symbol, entries),
            UpdateEpochSec(db, symbol, entries),
            UpdateLastMeta(db, symbol, Convert.ToDouble(entries[^1].Close)),
            db.SetAddAsync(SourcesInUseKey, symbol)
        );
    }

    public static async Task CreateNewBar(string symbol, DateTime timestamp) {
        var db = GetDatabase();

        var earliest = (await db.SortedSetPopAsync(symbol)).ToPxAtEpochMeta(symbol);
        var latest = (await db.SortedSetPopAsync(symbol, Order.Descending)).ToPxAtEpochMeta(symbol);

        if (!(await db.StringGetAsync(latest.Key)).TryParse(out double lastClose)) {
            throw new InvalidDataException(
                $"Failed to get the Px of {symbol} at {latest.EpochSec.ToDateTime().ToShortIso8601()}"
            );
        }

        await Task.WhenAll(
            Task.Run(
                async () => {
                    await db.KeyDeleteAsync(earliest.Key);
                    await db.StringSetAsync(KeyOfPxAtEpoch(symbol, timestamp.ToEpochSeconds()), lastClose);
                }
            ),
            CreateLastMeta(db, symbol, lastClose)
        );
    }

    public static async Task<IEnumerable<double>> GetRev(string symbol, int take) {
        var db = GetDatabase();

        var epochSecKeys = (await db.SortedSetRangeByScoreAsync(symbol, order: Order.Descending, take: take))
            .Select(
                (r, idx) => {
                    if (!r.TryParse(out long epochSec)) {
                        throw new InvalidDataException($"Failed to parse epoch second of {symbol} at #{idx}");
                    }

                    return new RedisKey(KeyOfPxAtEpoch(symbol, epochSec));
                }
            )
            .ToArray();

        return (await db.StringGetAsync(epochSecKeys))
            .Select(
                (r, idx) => {
                    if (!r.TryParse(out double lastPx)) {
                        throw new InvalidDataException(
                            $"Failed to parse last Px of {symbol} at key {epochSecKeys[idx]}"
                        );
                    }

                    return lastPx;
                }
            );
    }
}