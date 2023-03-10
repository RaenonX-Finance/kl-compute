using System.Collections.Immutable;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.Common.Utils;
using Serilog;
using StackExchange.Redis;

namespace KL.Common.Controllers;


public class RedisLastPxController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RedisLastPxController));

    private const string SourcesInUseKey = "Sources";

    public static async Task Initialize() {
        await RedisHelper.ClearDb(RedisDbId.LastPxAndMomentum);
    }

    private static string KeyOfSingleData(string symbol, long epochSec) {
        return $"{symbol}:{epochSec}";
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
                    KeyOfSingleData(symbol, r.Timestamp.ToEpochSeconds()),
                    (double)r.Close
                )
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

    public static async Task CreateNewBar(string symbol, DateTime timestamp) {
        var db = GetDatabase();
        
        var epochSecToRemove = await db.SortedSetPopAsync(symbol);

        if (epochSecToRemove == null) {
            Log.Warning("{Symbol} does not have px data, failed to create new bar", symbol);
            return;
        }

        var epochSec = (long)epochSecToRemove.Value.Score;
        var keyToRemove = KeyOfSingleData(symbol, epochSec);

        if (!(await db.StringGetAsync(keyToRemove)).TryParse(out double lastClose)) {
            throw new InvalidDataException(
                $"Failed to parse the Px of {symbol} at {epochSec.ToDateTime().ToShortIso8601()}"
            );
        }

        await db.KeyDeleteAsync(keyToRemove);
        await db.StringSetAsync(KeyOfSingleData(symbol, timestamp.ToEpochSeconds()), lastClose);
    }

    public static async Task<IEnumerable<double>> GetRev(string symbol, int take) {
        var db = GetDatabase();

        var epochSecKeys = (await db.SortedSetRangeByScoreAsync(symbol, order: Order.Descending, take: take))
            .Select(
                (r, idx) => {
                    if (!r.TryParse(out long epochSec)) {
                        throw new InvalidDataException($"Failed to parse epoch second of {symbol} at #{idx}");
                    }

                    return new RedisKey(KeyOfSingleData(symbol, epochSec));
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