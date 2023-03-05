using System.Collections.Immutable;
using System.Diagnostics;
using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Controllers;


public static class PxCacheController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PxCacheController));

    public static async Task Initialize() {
        await RedisLastPxController.Initialize();
    }

    public static async Task Create(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        var start = Stopwatch.GetTimestamp();
        Log.Information("Creating Px Cache of {Symbol}", symbol);

        var entriesToProcess = entries
            .TakeLast(PxConfigController.Config.Cache.InitCount)
            .ToImmutableArray();

        await RedisLastPxController.Set(symbol, entriesToProcess, isCreate: true);

        Log.Information(
            "Created Px Cache of {Symbol} ({Count} - last at {LastTimestamp}) in {Elapsed:0.00} ms",
            symbol,
            entriesToProcess.Length,
            entriesToProcess.Max(r => r.Timestamp),
            start.GetElapsedMs()
        );
    }

    public static async Task Update(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        var entriesToProcess = entries
            .TakeLast(PxConfigController.Config.Cache.UpdateCount)
            .ToImmutableArray();

        await RedisLastPxController.Set(symbol, entriesToProcess);
    }

    public static async Task CreateNewBar(DateTime timestamp) {
        var start = Stopwatch.GetTimestamp();
        Log.Information("Adding new bars at {NewBarTimestamp}", timestamp);

        var symbolsCreated = await RedisLastPxController.CreateNewBar(timestamp);

        Log.Information(
            "Added new bars for {@Symbols} at {NewBarTimestamp} in {Elapsed:0.00} ms",
            symbolsCreated,
            timestamp,
            start.GetElapsedMs()
        );
    }
}