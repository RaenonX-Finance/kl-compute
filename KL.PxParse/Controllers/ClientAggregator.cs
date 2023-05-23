using System.Diagnostics;
using KL.Common;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Extensions;
using KL.Common.Grpc;
using KL.Common.Interfaces;
using KL.PxParse.Grpc;
using KL.PxParse.Interfaces;
using KL.Touchance;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Controllers;


public class ClientAggregator : IClientAggregator {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ClientAggregator));

    private readonly CancellationToken _cancellationToken = new CancellationTokenSource().Token;

    private readonly TouchanceClient _touchanceClient;

    public ClientAggregator() {
        _touchanceClient = new TouchanceClient(_cancellationToken);

        _touchanceClient.HistoryDataUpdatedEventAsync += OnHistoryDataUpdated;
        _touchanceClient.RealtimeDataUpdatedEventAsync += OnRealtimeDataUpdated;
        _touchanceClient.InitCompletedEvent += OnInitCompleted;
        _touchanceClient.PxErrorEventAsync += OnPxError;
    }

    public async Task Start() {
        await PxCacheController.Initialize();
        await _touchanceClient.Start();
    }

    private async Task OnInitCompleted(object? sender, InitCompletedEventArgs e) {
        await GrpcPxDataCaller.CalcAll(e, _cancellationToken);

        // --- Attaching event handlers after the client has initialized ---
        // `OnMinuteChanged` might invoke before `GrpcPxDataCaller.CalcAll`
        // if data fetch is still in progress but minute changed
        // When this happen, `OnMinuteChanged` might not have data to calculate because calculated data is unavailable
        // --- Remove then add back to avoid duplicated event handling ---
        // If `subscribe` is called, `OnInitCompleted` is also called, so the event handler might be hooked twice
        _touchanceClient.MinuteChangeEventAsync -= OnMinuteChanged;
        _touchanceClient.MinuteChangeEventAsync += OnMinuteChanged;
    }

    private static async Task OnHistoryDataUpdatedStoreDb(HistoryEventArgs e) {
        IEnumerable<IHistoryDataEntry> dataToDb = e.Data;

        if (e.IsSubscription) {
            dataToDb = dataToDb.TakeLast(PxConfigController.Config.HistorySubscription.StoreLimit);
        }

        var entries = dataToDb
            .Select(r => r.ToHistoryDataModel(e.Metadata.Symbol, e.Metadata.Interval))
            .ToArray();

        if (e is { IsSubscription: true, IsMinuteChanged: false }) {
            HistoryDataController.UpdateAllBatched(e.Metadata.Symbol, e.Metadata.Interval, entries);
        } else {
            await HistoryDataController.UpdateAll(
                e.Metadata.Symbol,
                e.Metadata.Interval,
                entries,
                retryOnWriteConflict: true
            );
        }
    }

    private static async Task OnHistoryDataUpdatedUpdateCache(HistoryEventArgs e) {
        if (e.IsSubscription) {
            // Last 2nd data might get correction
            await PxCacheController.Update(e.Metadata.Symbol, e.Data);
        } else {
            // Non-subscription only happens on initialize, so creating cache at these time
            await PxCacheController.Create(e.Metadata.Symbol, e.Data);
        }
    }

    private static async Task OnHistoryDataUpdated(object? sender, HistoryEventArgs e) {
        var start = Stopwatch.GetTimestamp();

        if (e.Data.IsEmpty()) {
            Log.Warning(
                "[{Identifier}] Received empty history data, aborting further actions",
                e.Metadata.ToIdentifier()
            );
            return;
        }

        var last = e.Data[^1];

        Log.Information(
            "[{Identifier}] Received {Count} history data ({LastClose} @ {LastTimestamp})",
            e.Metadata.ToIdentifier(),
            e.Data.Count,
            last.Close,
            last.Timestamp
        );

        await Task.WhenAll(
            OnHistoryDataUpdatedStoreDb(e),
            OnHistoryDataUpdatedUpdateCache(e)
        );

        Log.Information(
            "Handled history data of {Symbol} ({Identifier}) in {Elapsed:0.00} ms",
            e.Metadata.Symbol,
            e.Metadata.ToIdentifier(),
            start.GetElapsedMs()
        );
    }

    private async Task OnRealtimeDataUpdated(object? sender, RealtimeEventArgs e) {
        var start = Stopwatch.GetTimestamp();

        if (!e.IsTriggeredByHistory) {
            // Avoid duplicated cache update because `onHistory` handler also stores px to cache
            await PxCacheController.Update(e.Symbol, e.Data.Close);
        }

        var isPxUpdated = await PxCacheController.IsUpdated(e.Symbol);

        if (isPxUpdated) {
            await Task.WhenAll(
                Task.Run(() => GrpcPxDataCaller.CalcLastAsync(e.Symbol, _cancellationToken), _cancellationToken),
                // Only call gRPC `realtime` if the realtime update is triggered by real realtime data
                // > open of `e.Data` might be an undesired value (current bar open instead of daily open)
                e.IsTriggeredByHistory ? 
                    Task.CompletedTask :
                    GrpcSystemEventCaller.OnRealtimeAsync(e.Symbol, e.Data, _cancellationToken)
            );
        }

        Log.Information(
            "Realtime data of {Symbol} ({Px}) {WillHandle} in {Elapsed:0.00} ms ({FillTimeDiff:0.00} ms)",
            e.Symbol,
            e.Data.Close,
            isPxUpdated ? "handled" : "skipped",
            start.GetElapsedMs(),
            e.Timestamp is not null ? (DateTime.UtcNow - e.Timestamp).Value.Milliseconds : double.NaN
        );
    }

    private async Task OnMinuteChanged(object? sender, MinuteChangeEventArgs e) {
        var start = Stopwatch.GetTimestamp();

        var tasks = new List<Task> {
            GrpcPxDataCaller.CalcPartial(e.Symbol, _cancellationToken),
            GrpcSystemEventCaller.OnMinuteChanged(e.Symbol, e.EpochSecond, _cancellationToken),
            PxCacheController.CreateNewBar(e.Symbol, e.Timestamp)
        };
        if (PxConfigController.IsTimestampMarketDateCutoff(e.Symbol, e.Timestamp)) {
            tasks.Add(GrpcSystemEventCaller.OnMarketDateCutoff(e.Symbol, _cancellationToken));
        }

        await Task.WhenAll(tasks);

        Log.Information(
            "Handled minute change of {Symbol} to {NewMinuteTimestamp} in {Elapsed:0.00} ms",
            e.Symbol,
            e.Timestamp,
            start.GetElapsedMs()
        );
    }

    private Task OnPxError(object? sender, PxErrorEventArgs e) {
        GrpcSystemEventCaller.OnErrorAsync(e.Message, _cancellationToken);

        Log.Information("Received error message: {Message}", e.Message);

        return Task.CompletedTask;
    }

    public Task<bool> Subscribe(IEnumerable<string> symbols, OnUpdate onUpdate) {
        return _touchanceClient.InitializeSources(
            PxConfigController.Config.SourceList
                .Where(r => r is { Enabled: true, Source: PxSource.Touchance } && symbols.Contains(r.InternalSymbol))
                .ToArray(),
            onUpdate
        );
    }
}