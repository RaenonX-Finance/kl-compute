using System.Text.Json;
using KL.Common.Controllers;
using KL.Common.Events;
using KL.Touchance.Extensions;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Subscriptions;
using KL.Touchance.Utils;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace KL.Touchance.Handlers;


internal class SubscriptionHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SubscriptionHandler));

    internal required TouchanceClient Client { get; init; }

    internal required RealtimeHandler RealtimeHandler { get; init; }

    internal required HistoryDataHandler HistoryDataHandler { get; init; }

    public required MinuteChangedHandler MinuteChangedHandler { get; init; }

    public void StartAsync(int subscriberPort, CancellationToken cancellationToken) {
        Task.Run(() => Start(subscriberPort, Client, cancellationToken), cancellationToken);
    }

    private async Task HandleSubscriptionMessage(
        string messageJson,
        CancellationToken cancellationToken
    ) {
        var tcSubscription = messageJson.ToTcSubscription();

        Log.Debug("Received subscription message of type {Type}", tcSubscription.GetType());

        switch (tcSubscription) {
            case PingMessage:
                Client.RequestSocket.SendTcRequest<PongRequest, PongReply>(
                    new PongRequest {
                        SessionKey = Client.SessionKey,
                        Id = "TC"
                    }
                );
                return;
            case PxRealtimeMessage message:
                var realtimeEventArgs = RealtimeHandler.ToEventArgs(message);
                
                if (realtimeEventArgs is null) {
                    return;
                }
                
                Client.OnRealtimeDataUpdated(realtimeEventArgs);
                return;
            case PxHistoryReadyMessage message:
                var historyEventArgs = HistoryDataHandler.GetHistoryData(message, cancellationToken);

                if (historyEventArgs is null) {
                    return;
                }

                await Client.OnHistoryDataUpdated(historyEventArgs);

                // Minute change needs to be placed AFTER history event
                // History event handler could add a new bar, which is to be used by minute changed event 
                if (historyEventArgs.Data.Count > 0) {
                    MinuteChangedHandler.CheckMinuteChangedEvent(
                        historyEventArgs.Metadata.Symbol,
                        historyEventArgs.Data[^1].Timestamp
                    );
                }

                return;
            case MinuteChangeMessage:
                // Not using Touchance minute change event because it could trigger
                // before history data actually logs minute change
                // > If minute change is triggered before history data actually gets new bar,
                // history data grouper will be called with the latest data in previous minute,
                // causing minute freeze in calculated data, but not on history data
                return;
            case SymbolClearMessage message:
                await Client.OnSymbolCleared(message);
                return;
            default:
                Log.Warning("Unhandled subscription message: {Message}", messageJson);
                return;
        }
    }

    private async Task Start(int subscriberPort, PxParseClient client, CancellationToken cancellationToken) {
        var socketConnectionString = $">tcp://127.0.0.1:{subscriberPort}";
        using var subscriberSocket = new SubscriberSocket(socketConnectionString);
        subscriberSocket.SubscribeToAnyTopic();

        Log.Information("Subscribed to {ConnectionString}", socketConnectionString);

        while (!cancellationToken.IsCancellationRequested) {
            var messageJson = subscriberSocket
                .ReceiveFrameString()
                // Only care about the message after 1st colon
                .Split(":", 2)[1];

            try {
                await HandleSubscriptionMessage(messageJson, cancellationToken);
            } catch (JsonException ex) {
                client.OnPxError(
                    new PxErrorEventArgs {
                        Message = "Unable to process JSON message"
                    }
                );
                Log.Error(ex, "Unable to process JSON message: {Message}", messageJson);
                throw;
            }
        }
    }
}