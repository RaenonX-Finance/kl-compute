﻿using KL.Common;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Extensions;
using KL.Common.Models.Config;
using KL.Common.Utils;
using KL.Touchance.Extensions;
using KL.Touchance.Handlers;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Subscriptions;
using NetMQ.Sockets;
using Serilog;

namespace KL.Touchance;


public class TouchanceClient : PxParseClient {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(TouchanceClient));

    public readonly RequestSocket RequestSocket
        = new($">tcp://127.0.0.1:{EnvironmentConfigHelper.Config.Source.Touchance.ZmqPort}");

    private readonly HistoryDataHandler _historyDataHandler;

    private readonly RealtimeHandler _realtimeHandler;

    private readonly SubscriptionHandler _subscriptionHandler;

    private static SemaphoreSlim? _semaphore;

    private string? _sessionKeyInternal;

    public string SessionKey {
        get {
            if (_sessionKeyInternal is null) {
                throw new InvalidOperationException("Touchance not connected - empty session key");
            }

            return _sessionKeyInternal;
        }
    }

    public TouchanceClient(CancellationToken cancellationToken) : base(true, cancellationToken) {
        _historyDataHandler = new HistoryDataHandler { Client = this };
        _realtimeHandler = new RealtimeHandler { Client = this };
        _subscriptionHandler = new SubscriptionHandler {
            Client = this,
            HistoryDataHandler = _historyDataHandler,
            RealtimeHandler = _realtimeHandler,
            MinuteChangedHandler = new MinuteChangedHandler()
        };
    }

    public async Task Start() {
        Log.Information("Starting Touchance");
        var loginReply = RequestSocket.SendTcRequest<LoginRequest, LoginReply>(
            new LoginRequest {
                Param = new LoginRequestParams {
                    SystemName = "ZMQ",
                    ServiceKey = "8076c9867a372d2a9a814ae710c256e2"
                }
            },
            new TimeSpan(0, 0, 0, EnvironmentConfigHelper.Config.Source.Touchance.Timeout.Login)
        );

        if (loginReply is null) {
            Log.Error(
                "Touchance did not respond from port {TouchancePort} in {LoginTimeoutSec} seconds, terminating",
                EnvironmentConfigHelper.Config.Source.Touchance.ZmqPort,
                EnvironmentConfigHelper.Config.Source.Touchance.Timeout.Login
            );
            Environment.Exit(1);
        }

        Log.Information(
            "Touchance logged in - Session [{Session}] Subscription Port [{SubPort}]",
            loginReply.SessionKey,
            loginReply.SubPort
        );

        _sessionKeyInternal = loginReply.SessionKey;

        // Needs to start getting subscription before `Initialize()`
        // because `Initialize()` needs the content in the subscribing channel
        _subscriptionHandler.StartAsync(loginReply.SubPort, CancellationToken);

        await Initialize();
    }

    private async Task Initialize() {
        var sources = PxConfigController.Config.SourceList
            .Where(r => r is { Enabled: true, Source: PxSource.Touchance })
            .ToArray();

        await InitializeSources(sources);
    }

    // CA1822 False-positive - This function cannot be static because its overload cannot be static
    #pragma warning disable CA1822
    private Task<bool> InitializeSources(IList<PxSourceConfigModel> sources) {
        return InitializeSources(sources, _ => Task.CompletedTask);
    }
    #pragma warning restore CA1822

    public async Task<bool> InitializeSources(IList<PxSourceConfigModel> sources, OnUpdate onUpdate) {
        if (sources.IsEmpty()) {
            Log.Warning("Attempt to subscribe data but `sources` is empty");
            return false;
        }

        await onUpdate($"Attempt to subscribe [{string.Join(", ", sources.Select(r => r.InternalSymbol))}]");

        await Task.WhenAll(
            this.CheckSourceInfo(sources),
            InitializeHistoryData(sources, onUpdate)
        );

        await OnInitCompleted(new InitCompletedEventArgs { Sources = sources, OnUpdate = onUpdate });

        await SendDataSubscriptionRequest(sources, onUpdate);
        return true;
    }

    private async Task InitializeHistoryData(IEnumerable<PxSourceConfigModel> sources, OnUpdate onUpdate) {
        var isAnyPeriodDaily = PxConfigController.Config.Periods.Any(
            period => period.PeriodMin.GetHistoryInterval() == HistoryInterval.Daily
        );

        _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        using (_semaphore) {
            // Request all history data
            foreach (var source in sources) {
                await _semaphore.WaitAsync();
                await Task.WhenAll(
                    onUpdate($"Requesting minute history data of {source.ExternalSymbol}"),
                    _historyDataHandler.SendHandshakeRequest(
                        source.ExternalSymbol,
                        HistoryInterval.Minute,
                        DateTime.UtcNow.AddDays(
                            -PxConfigController.Config.InitDataBacktrackDays[HistoryInterval.Minute]
                        ),
                        // Need to fetch the history data until the very last available bar
                        DateTime.UtcNow.AddHours(1),
                        false
                    )
                );

                if (!isAnyPeriodDaily) {
                    continue;
                }

                await _semaphore.WaitAsync();
                await Task.WhenAll(
                    onUpdate($"Requesting daily history data of {source.ExternalSymbol}"),
                    _historyDataHandler.SendHandshakeRequest(
                        source.ExternalSymbol,
                        HistoryInterval.Daily,
                        DateTime.UtcNow.AddDays(
                            -PxConfigController.Config.InitDataBacktrackDays[HistoryInterval.Daily]
                        ),
                        // Need to fetch the history data until the very last available bar
                        DateTime.UtcNow.AddDays(1),
                        false
                    )
                );
            }

            // Wait until history data requests are completed
            await _semaphore.WaitAsync();
        }

        _semaphore = null;
    }

    // CA1822 False-positive - This function cannot be static because its overload cannot be static
    #pragma warning disable CA1822
    private Task SendDataSubscriptionRequest(IEnumerable<PxSourceConfigModel> sources) {
        return SendDataSubscriptionRequest(sources, _ => Task.CompletedTask);
    }
    #pragma warning restore CA1822

    private async Task SendDataSubscriptionRequest(IEnumerable<PxSourceConfigModel> sources, OnUpdate onUpdate) {
        foreach (var source in sources) {
            if (!source.Enabled) {
                await onUpdate(
                    $"Skipped sending data subscription request of {source.ExternalSymbol} (source not enabled)"
                );
                Log.Information(
                    "Skipped sending data subscription request of {Symbol} (source not enabled)",
                    source.ExternalSymbol
                );
                continue;
            }

            await onUpdate($"Subscribing price data of {source.ExternalSymbol}");

            if (source.EnableRealtime) {
                _realtimeHandler.SubscribeRealtime(source.ExternalSymbol);
            }

            Log.Information("Subscribing history data of {Symbol}", source.ExternalSymbol);
            await _historyDataHandler.SendHandshakeRequest(
                source.ExternalSymbol,
                HistoryInterval.Minute,
                DateTime.UtcNow.AddHours(-PxConfigController.Config.HistorySubscription.InitialBufferHrs),
                // Needs to be 2 because symbol clear happens before the trading hour starts
                // If this is `1`, say the symbol clear happens at 6, then the request would be ending at 7.
                // Then, when the actual trading sessions starts, the request won't get any data.
                DateTime.UtcNow.AddHours(2),
                true
            );
        }
    }

    public async Task OnSymbolCleared(SymbolClearMessage message) {
        Log.Information("Received symbol clear for {Symbol}, resubscribing...", message.Data.Symbol);

        // Should not try to resubscribe FITX in same symbol clear event
        if (message.Data.Symbol != "TC.F.TWF.FITX") {
            // `TWF` symbols need manual re-subscription for the open before 8:45 AM (UTC +8)
            // according to Touchance Customer Service
            await SendDataSubscriptionRequest(
                PxConfigController.Config.SourceList
                    .Where(r => r.Source == PxSource.Touchance && r.ExternalSymbol.Contains("TWF"))
            );
        }

        // Searching symbols to resubscribe because
        // `message.Data.Symbol` is in the format of `TC.F.CME.NQ`,
        // but the symbol to subscribe needs to be `TC.F.CME.NQ.HOT`
        await SendDataSubscriptionRequest(
            PxConfigController.Config.SourceList
                .Where(r => r.Source == PxSource.Touchance && r.ExternalSymbol.Contains(message.Data.Symbol))
        );
    }

    protected override void OnHistoryDataUpdatedCompleted(HistoryEventArgs e) {
        if (_semaphore is not null && !e.IsSubscription) {
            _semaphore.Release();
        }
    }
}