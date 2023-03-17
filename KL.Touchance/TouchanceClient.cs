using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
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

    private readonly SubscriptionHandler _subscriptionHandler;

    private readonly HistoryDataHandler _historyDataHandler;

    private readonly RealtimeHandler _realtimeHandler;

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
        _subscriptionHandler = new SubscriptionHandler {
            Client = this,
            HistoryDataHandler = _historyDataHandler,
            MinuteChangedHandler = new MinuteChangedHandler { Client = this }
        };
        _realtimeHandler = new RealtimeHandler { Client = this };
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
            new TimeSpan(0, 0, 0, EnvironmentConfigHelper.Config.Source.Touchance.LoginTimeout)
        );

        if (loginReply is null) {
            Log.Error(
                "Touchance did not respond from port {TouchancePort} in {LoginTimeoutSec} seconds, terminating",
                EnvironmentConfigHelper.Config.Source.Touchance.ZmqPort,
                EnvironmentConfigHelper.Config.Source.Touchance.LoginTimeout
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
        var sources = PxConfigController.Config.Sources
            .Where(r => r is { Enabled: true, Source: PxSource.Touchance })
            .ToArray();

        await Task.WhenAll(
            this.CheckSourceInfo(sources),
            InitializeHistoryData(sources)
        );

        await OnInitCompleted(new InitCompletedEventArgs { SourcesInUse = sources });

        var symbols = sources.Select(r => r.ExternalSymbol).ToArray();
        
        SendHistorySubscriptionRequest(symbols);
        SendRealtimeSubscriptionRequest(symbols);
    }

    private async Task InitializeHistoryData(IEnumerable<PxSourceConfigModel> sources) {
        var isAnyPeriodDaily = PxConfigController.Config.Periods.Any(
            period => period.PeriodMin.GetHistoryInterval() == HistoryInterval.Daily
        );

        _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        using (_semaphore) {
            // Request all history data
            foreach (var source in sources) {
                await _semaphore.WaitAsync();
                _historyDataHandler.SendHandshakeRequest(
                    source.ExternalSymbol,
                    HistoryInterval.Minute,
                    DateTime.UtcNow.AddDays(-PxConfigController.Config.InitDataBacktrackDays[HistoryInterval.Minute]),
                    // Need to fetch the history data until the very last available bar
                    DateTime.UtcNow.AddHours(1),
                    false
                );

                if (!isAnyPeriodDaily) {
                    continue;
                }

                await _semaphore.WaitAsync();
                _historyDataHandler.SendHandshakeRequest(
                    source.ExternalSymbol,
                    HistoryInterval.Daily,
                    DateTime.UtcNow.AddDays(-PxConfigController.Config.InitDataBacktrackDays[HistoryInterval.Daily]),
                    DateTime.UtcNow,
                    false
                );
            }

            // Wait until history data requests are completed
            await _semaphore.WaitAsync();
        }

        _semaphore = null;
    }

    private void SendHistorySubscriptionRequest(IEnumerable<string> touchanceSymbols) {
        foreach (var symbol in touchanceSymbols) {
            _historyDataHandler.SendHandshakeRequest(
                symbol,
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

    private void SendRealtimeSubscriptionRequest(IEnumerable<string> touchanceSymbols) {
        foreach (var symbol in touchanceSymbols) {
            if (!PxConfigController.Config.Sources
                    .First(r => r.ExternalSymbol == symbol && r.Source == PxSource.Touchance)
                    .EnableRealtime) {
                continue;
            }
            _realtimeHandler.SubscribeRealtime(symbol);
        }
    }

    public void OnSymbolCleared(SymbolClearMessage message) {
        Log.Information("Received symbol clear for {Symbol}, resubscribing...", message.Data.Symbol);

        // Should not try to resubscribe FITX in same symbol clear event
        if (message.Data.Symbol != "TC.F.TWF.FITX") {
            // `TWF` symbols need manual re-subscription for the open before 8:45 AM (UTC +8)
            // according to Touchance Customer Service
            var twfSymbols = PxConfigController.Config.Sources
                .Where(r => r.Source == PxSource.Touchance && r.ExternalSymbol.Contains("TWF"))
                .Select(r => r.ExternalSymbol);
            SendHistorySubscriptionRequest(twfSymbols);
        }

        // Searching symbols to resubscribe because
        // `message.Data.Symbol` is in the format of `TC.F.CME.NQ`,
        // but the symbol to subscribe needs to be `TC.F.CME.NQ.HOT`
        SendHistorySubscriptionRequest(
            PxConfigController.Config.Sources
                .Where(r => r.Source == PxSource.Touchance && r.ExternalSymbol.Contains(message.Data.Symbol))
                .Select(r => r.ExternalSymbol)
        );
    }

    protected override void OnHistoryDataUpdatedCompleted(HistoryEventArgs e) {
        if (_semaphore is not null && !e.IsSubscription) {
            _semaphore.Release();
        }
    }
}