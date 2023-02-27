﻿using System.Collections.Immutable;
using System.Diagnostics;
using KL.Common.Controllers;
using KL.Common.Events;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.PxParse.Utils;
using KL.Touchance;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Controllers;


public class ClientAggregator {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(ClientAggregator));

    private readonly TouchanceClient _touchanceClient;

    private readonly CancellationToken _cancellationToken;

    public ClientAggregator(CancellationToken cancellationToken) {
        _cancellationToken = cancellationToken;

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
        await GrpcHelper.CalcAll(e.SourcesInUse.Select(r => r.InternalSymbol), _cancellationToken);

        // --- Attaching these after the client has initialized ---
        // `OnMinuteChanged` might invoke before `GrpcHelper.CalcAll`.
        // When this happen, `OnMinuteChanged` might not have data to calculate because calculated data is unavailable.
        _touchanceClient.MinuteChangeEventAsync += OnMinuteChanged;
    }

    private static async Task OnHistoryDataUpdatedStoreDb(HistoryEventArgs e) {
        IEnumerable<IHistoryDataEntry> dataToDb = e.Data;

        if (e.IsSubscription) {
            dataToDb = dataToDb.TakeLast(PxConfigController.Config.HistorySubscription.StoreLimit);
        }

        await HistoryDataController.UpdateAll(
            e.Metadata.Symbol,
            e.Metadata.Interval,
            dataToDb.Select(r => r.ToHistoryDataModel(e.Metadata.Symbol, e.Metadata.Interval)).ToImmutableList()
        );
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
            "Handled history data [{Identifier}] in {ElapsedMs:0.00} ms",
            e.Metadata.ToIdentifier(),
            start.GetElapsedMs()
        );
    }

    private Task OnRealtimeDataUpdated(object? sender, RealtimeEventArgs e) {
        // Not putting cache updating call here because this event is currently triggered on receiving history data
        GrpcHelper.CalcLastAsync(e.Symbol, _cancellationToken);

        return Task.CompletedTask;
    }
    
    private async Task OnMinuteChanged(object? sender, MinuteChangeEventArgs e) {
        var start = Stopwatch.GetTimestamp();

        GrpcHelper.CalcPartialAsync(
            PxConfigController.Config.Sources.Where(r => r.Enabled).Select(r => r.InternalSymbol),
            _cancellationToken
        );
        await PxCacheController.CreateNewBar(e.Timestamp);

        // TODO: - [OnMinuteChanged] gRPC: to trigger minute change event & (Frontend) client to automatically send request for data
        Log.Information(
            "Handled minute change to {NewMinuteTimestamp} in {ElapsedMs:0.00} ms",
            e.Timestamp,
            start.GetElapsedMs()
        );
    }

    private static Task OnPxError(object? sender, PxErrorEventArgs e) {
        // TODO: - [OnPxError] gRPC: to send error event
        Log.Information("OnPxError");

        return Task.CompletedTask;
    }
}