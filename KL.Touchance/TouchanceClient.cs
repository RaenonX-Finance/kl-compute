using System.Collections.Immutable;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Models.Config;
using KL.Touchance.Extensions;
using KL.Touchance.Handlers;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using NetMQ.Sockets;
using Serilog;

namespace KL.Touchance;


public class TouchanceClient : PxParseClient {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(TouchanceClient));

    public static readonly RequestSocket RequestSocket = new(">tcp://127.0.0.1:51237");

    private static SemaphoreSlim? _semaphore;

    private static string? _sessionKeyInternal;

    public TouchanceClient(CancellationToken cancellationToken) : base(cancellationToken) { }

    public static string SessionKey {
        get {
            if (_sessionKeyInternal == null) {
                throw new InvalidOperationException("Touchance not connected - empty session key");
            }

            return _sessionKeyInternal;
        }
    }

    private static async Task InitializeHistoryData(IImmutableList<PxSourceConfigModel> sources) {
        var isAnyPeriodDaily = PxConfigController.Config.Periods.Any(
            period => period.PeriodMin.GetHistoryInterval() == HistoryInterval.Daily
        );

        _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        using (_semaphore) {
            // Request all history data
            foreach (var source in sources) {
                await _semaphore.WaitAsync();
                HistoryDataHandler.SendHandshakeRequest(
                    source.ExternalSymbol,
                    HistoryInterval.Minute,
                    DateTime.UtcNow.AddDays(-PxConfigController.Config.InitDataBacktrackDays[HistoryInterval.Minute]),
                    DateTime.UtcNow,
                    false
                );

                if (!isAnyPeriodDaily) {
                    continue;
                }

                await _semaphore.WaitAsync();
                HistoryDataHandler.SendHandshakeRequest(
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

    private static void InitializeSubscribeHistoryData(IImmutableList<PxSourceConfigModel> sources) {
        // Subscribe history data
        foreach (var source in sources) {
            HistoryDataHandler.SendHandshakeRequest(
                source.ExternalSymbol,
                HistoryInterval.Minute,
                DateTime.UtcNow.AddHours(-PxConfigController.Config.HistorySubscription.InitialBufferHrs),
                // Adding 1 hour to ensure getting the latest history data
                DateTime.UtcNow.AddHours(1),
                true
            );
        }
    }

    private async Task Initialize() {
        var sources = PxConfigController.Config.Sources
            .Where(
                r => r is {
                    Enabled: true, Source: PxSource.Touchance
                }
            )
            .ToImmutableList();

        await Task.WhenAll(
            SourceInfoHandler.CheckSourceInfo(sources),
            InitializeHistoryData(sources)
        );
        
        await OnInitCompleted(new InitCompletedEventArgs { SourcesInUse = sources });

        InitializeSubscribeHistoryData(sources);
    }

    public async Task Start() {
        var loginReply = RequestSocket.SendTcRequest<LoginRequest, LoginReply>(
            new LoginRequest {
                Param = new LoginRequestParams {
                    SystemName = "ZMQ",
                    ServiceKey = "8076c9867a372d2a9a814ae710c256e2"
                }
            }
        );

        Log.Information(
            "Touchance logged in - Session [{Session}] Subscription Port [{SubPort}]",
            loginReply.SessionKey,
            loginReply.SubPort
        );

        _sessionKeyInternal = loginReply.SessionKey;

        // Needs to start getting subscription before `Initialize()`
        // because `Initialize()` needs the content in the subscribing channel
        SubscriptionHandler.StartAsync(loginReply.SubPort, this, CancellationToken);

        await Initialize();
    }

    protected override void OnHistoryDataUpdatedCompleted(HistoryEventArgs e) {
        if (_semaphore != null && !e.IsSubscription) {
            _semaphore.Release();
        }
    }
}