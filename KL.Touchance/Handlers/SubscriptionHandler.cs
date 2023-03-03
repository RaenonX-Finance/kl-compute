using System.Text.Json;
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


public class SubscriptionHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SubscriptionHandler));

    public required TouchanceClient Client { get; init; }

    public required HistoryDataHandler HistoryDataHandler { get; init; }
    
    public void StartAsync(int subscriberPort, CancellationToken cancellationToken) {
        new Thread(() => Start(subscriberPort, Client, cancellationToken)).Start();
    }

    private void HandleSubscriptionMessage(
        string messageJson,
        CancellationToken cancellationToken
    ) {
        var tcSubscription = messageJson.ToTcSubscription();

        Log.Information("Received subscription message of type {Type}", tcSubscription.GetType());

        switch (tcSubscription) {
            case PingMessage:
                Client.RequestSocket.SendTcRequest<PongRequest, PongReply>(
                    new PongRequest {
                        SessionKey = Client.SessionKey,
                        Id = "TC"
                    }
                );
                return;
            case PxHistoryReadyMessage message:
                var eventArgs = HistoryDataHandler.GetHistoryData(message, cancellationToken);
                if (eventArgs == null) {
                    return;
                }

                Client.OnHistoryDataUpdated(eventArgs);
                break;
            case MinuteChangeMessage message:
                Client.OnMinuteChanged(new MinuteChangeEventArgs { Timestamp = message.GetTimestamp() });
                return;
            case SymbolClearMessage message:
                // Using `SymbolToSubscribe` instead of `Data.Symbol` because
                // `Data.Symbol` is in the format of `TC.F.CME.NQ`,
                // but the symbol to subscribe needs to be `TC.F.CME.NQ.HOT`
                Log.Information("Received symbol clear for {Symbol}, resubscribing...", message.Data.Symbol);
                Client.SendHistorySubscriptionRequest(message.SymbolToSubscribe);
                return;
            default:
                Log.Warning("Unhandled subscription message: {Message}", messageJson);
                return;
        }
    }

    private void Start(int subscriberPort, TouchanceClient client, CancellationToken cancellationToken) {
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
                HandleSubscriptionMessage(messageJson, cancellationToken);
            } catch (JsonException) {
                client.OnPxError(
                    new PxErrorEventArgs {
                        Message = "Unable to process JSON message"
                    }
                );
                Log.Error("Unable to process JSON message: {Message}", messageJson);
                throw;
            }
        }
    }
}