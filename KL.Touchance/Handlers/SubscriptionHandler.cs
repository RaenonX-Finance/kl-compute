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


public class SubscriptionHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SubscriptionHandler));

    public static void StartAsync(int subscriberPort, PxParseClient client, CancellationToken cancellationToken) {
        new Thread(() => Start(subscriberPort, client, cancellationToken)).Start();
    }

    private static void Start(int subscriberPort, PxParseClient client, CancellationToken cancellationToken) {
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
                var tcSubscription = messageJson.ToTcSubscription();

                Log.Information("Received subscription message of type {Type}", tcSubscription.GetType());

                switch (tcSubscription) {
                    case PingMessage:
                        TouchanceClient.RequestSocket.SendTcRequest<PongRequest, PongReply>(
                            new PongRequest {
                                SessionKey = TouchanceClient.SessionKey,
                                Id = "TC"
                            }
                        );
                        break;
                    case PxHistoryReadyMessage message:
                        var eventArgs = HistoryDataHandler.GetHistoryData(message, cancellationToken);
                        if (eventArgs == null) {
                            continue;
                        }
                        client.OnHistoryDataUpdated(eventArgs);
                        break;
                    case MinuteChangeMessage message:
                        client.OnMinuteChanged(new MinuteChangeEventArgs { Timestamp = message.GetTimestamp() });
                        break;
                    default:
                        Log.Warning("Unhandled subscription message: {Message}", messageJson);
                        break;
                }
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