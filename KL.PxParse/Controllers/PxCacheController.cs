using System.Diagnostics;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Controllers;


public static class PxCacheController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PxCacheController));

    public static async Task Initialize() {
        Log.Information("Initializing Px cache");
        await RedisLastPxController.Initialize();
    }

    public static async Task Create(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        var start = Stopwatch.GetTimestamp();
        Log.Information("Creating Px Cache of {Symbol}", symbol);

        var entriesToProcess = entries
            .TakeLast(PxConfigController.Config.Cache.InitCount)
            .ToList();

        if (entriesToProcess.Count < PxConfigController.Config.Cache.InitCount) {
            var dbGetLimit = PxConfigController.Config.Cache.InitCount - entriesToProcess.Count;
            var dbGetMaxTime = entriesToProcess.Select(r => r.Timestamp).Min();

            Log.Information(
                "Insufficient data to create Px cache of {Symbol} ({CountInEntries}), "
                + "getting {CountFromDB} history data before {MaxTimeInEntries} from DB",
                symbol,
                entriesToProcess.Count,
                dbGetLimit,
                dbGetMaxTime
            );
            entriesToProcess.AddRange(
                HistoryDataController.GetBeforeTime(symbol, HistoryInterval.Minute, dbGetMaxTime, dbGetLimit)
            );
        }

        await RedisLastPxController.Set(symbol, entriesToProcess, isCreate: true);

        Log.Information(
            "Created Px cache of {Symbol} ({Count} - last at {LastTimestamp}) in {Elapsed:0.00} ms",
            symbol,
            entriesToProcess.Count,
            entriesToProcess.Max(r => r.Timestamp),
            start.GetElapsedMs()
        );
    }

    public static async Task Update(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        var entriesToProcess = entries
            .TakeLast(PxConfigController.Config.Cache.UpdateCount)
            .ToArray();

        await RedisLastPxController.Set(symbol, entriesToProcess);
    }

    public static async Task Update(string symbol, decimal px) {
        await RedisLastPxController.Set(symbol, px);
    }

    public static async Task CreateNewBar(string symbol, DateTime timestamp) {
        var start = Stopwatch.GetTimestamp();

        await RedisLastPxController.CreateNewBar(symbol, timestamp);

        Log.Information(
            "Added new bar for {Symbol} at {NewBarTimestamp} in {Elapsed:0.00} ms",
            symbol,
            timestamp,
            start.GetElapsedMs()
        );
    }

    public static Task<bool> IsUpdated(string symbol) {
        return RedisLastPxController.PopUpdated(symbol);
    }
}