using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.Common.Utils;
using StackExchange.Redis;

namespace KL.Common.Controllers;


internal record PxAtEpochMeta {
    public required string Key { get; init; }
}

internal static class PxCloseDataExtension {
    public static PxAtEpochMeta ToPxAtEpochMeta(this SortedSetEntry? entry, string symbol) {
        if (entry is null) {
            throw new InvalidDataException($"{symbol} does not have px data, failed to create new bar");
        }

        var epochSec = (long)entry.Value.Score;
        var key = RedisLastPxController.KeyOfPxAtEpoch(symbol, epochSec);

        return new PxAtEpochMeta { Key = key };
    }
}

public static class RedisLastPxController {
    private const string SourcesInUseKey = "Sources";

    private const string LastMetaMinName = "Min";
    private const string LastMetaMaxName = "Max";
    private const string LastMetaLastName = "Last";
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
        IEnumerable<IHistoryDataEntry> entries
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

    private static async Task UpdatePx(IDatabaseAsync db, string symbol, IEnumerable<IHistoryDataEntry> entries) {
        var lastPxValues = entries.Select(
                r => new KeyValuePair<RedisKey, RedisValue>(
                    KeyOfPxAtEpoch(symbol, r.Timestamp.ToEpochSeconds()),
                    (double)r.Close
                )
            )
            .ToArray();

        await db.StringSetAsync(lastPxValues);
    }

    private static async Task UpdatePx(IDatabaseAsync db, string symbol, double px) {
        var latestEpochEntry = (await db.SortedSetRangeByScoreAsync(symbol, order: Order.Descending, take: 1)).First();

        if (!latestEpochEntry.TryParse(out long epochSec)) {
            throw new InvalidDataException($"Failed to parse epoch second of {symbol}");
        }

        await db.StringSetAsync(KeyOfPxAtEpoch(symbol, epochSec), px);
    }

    private static async Task CreateLastMeta(IDatabaseAsync db, string symbol, double px) {
        // Overwrites `lastMeta` - this is expected to call for every minute change
        await db.HashSetAsync(
            KeyOfLastMeta(symbol),
            new[] {
                new HashEntry(LastMetaMinName, px),
                new HashEntry(LastMetaMaxName, px),
                new HashEntry(LastMetaLastName, px),
                new HashEntry(LastMetaEpochMsName, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                new HashEntry(LastMetaUpdatedName, true)
            }
        );
    }

    private static async Task UpdateLastMeta(IDatabaseAsync db, string symbol, double px) {
        var key = KeyOfLastMeta(symbol);
        var meta = (await db.HashGetAllAsync(key))
            .ToDictionary(r => r.Name, r => r.Value);

        if (meta.IsEmpty()) {
            await CreateLastMeta(db, symbol, px);
            return;
        }

        var update = false;
        var nowEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var redisEntries = new List<HashEntry> { new(LastMetaLastName, px) };

        if (
            // Updated flag not popped yet (still true)
            (meta[LastMetaUpdatedName].TryParse(out int updatedInt) && Convert.ToBoolean(updatedInt))
            // Price changed and over certain period of time not updated
            || (meta[LastMetaLastName].TryParse(out long last)
                && Math.Abs(last - px) > 1E-6
                && meta[LastMetaEpochMsName].TryParse(out long epochMs)
                && nowEpochMs - epochMs > PxConfigController.Config.Cache.MarketUpdateGapMs
            )
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

    public static async Task Set(string symbol, IList<IHistoryDataEntry> entries, bool isCreate = false) {
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

    public static async Task Set(string symbol, decimal px) {
        var db = GetDatabase();
        var pxDouble = Convert.ToDouble(px);

        await Task.WhenAll(
            UpdatePx(db, symbol, pxDouble),
            UpdateLastMeta(db, symbol, pxDouble),
            db.SetAddAsync(SourcesInUseKey, symbol)
        );
    }

    public static async Task CreateNewBar(string symbol, DateTime timestamp) {
        var db = GetDatabase();

        var earliest = (await db.SortedSetPopAsync(symbol)).ToPxAtEpochMeta(symbol);

        if (!(await db.HashGetAsync(KeyOfLastMeta(symbol), LastMetaLastName)).TryParse(out double lastClose)) {
            throw new InvalidDataException($"Failed to get the Px of {symbol} from last meta");
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