using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Extensions;
using KL.Common.Interfaces;
using KL.Touchance.Extensions;
using KL.Touchance.Models;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Subscriptions;
using Serilog;

namespace KL.Touchance.Handlers;


public class HistoryDataHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataHandler));

    // Has key = request to handle
    // Value is true = is history data subscription
    private static readonly IDictionary<PxHistoryRequestIdentifier, bool> SubscribedRequests
        = new Dictionary<PxHistoryRequestIdentifier, bool>();

    public required TouchanceClient Client { get; init; }

    private void SendHandshakeRequest(
        PxHistoryRequestIdentifier identifier,
        PxHistoryHandshakeRequestParams @params,
        bool isSubscription
    ) {
        SubscribedRequests.Add(identifier, isSubscription);
        Client.RequestSocket.SendTcRequest<PxHistoryHandshakeRequest, PxHistoryHandshakeReply>(
            new PxHistoryHandshakeRequest { SessionKey = Client.SessionKey, Param = @params }
        );
    }

    internal void SendHandshakeRequest(
        string touchanceSymbol,
        HistoryInterval interval,
        DateTime start,
        DateTime end,
        bool isSubscription
    ) {
        if (start > end) {
            Log.Error("Start time ({Start}) must be earlier than the end time ({End})", start, end);
            throw new InvalidOperationException($"Start time ({start}) must be earlier than the end time ({end})");
        }

        var startTime = start.ToTouchanceHourlyPrecision();
        var endTime = end.ToTouchanceHourlyPrecision();

        var identifier = new PxHistoryRequestIdentifier {
            Symbol = touchanceSymbol,
            Interval = interval,
            Start = startTime.FromTouchanceHourlyPrecision(),
            End = endTime.FromTouchanceHourlyPrecision()
        };
        var @params = new PxHistoryHandshakeRequestParams {
            Symbol = touchanceSymbol,
            SubDataType = interval.GetTouchanceType(),
            StartTime = startTime,
            EndTime = endTime
        };

        Log.Information(
            "[{Identifier}] {Action} history data",
            ((IHistoryMetadata)identifier).ToIdentifier(),
            isSubscription ? "Subscribing" : "Requesting"
        );

        // Send unsubscribe first to ensure successful subscription
        SendUnsubscribeRequest(identifier, @params);
        SendHandshakeRequest(identifier, @params, isSubscription);
    }

    private void SendUnsubscribeRequest(PxHistoryReadyMessage message) {
        SendUnsubscribeRequest(
            message.Identifier,
            new PxHistoryHandshakeRequestParams {
                Symbol = message.Symbol,
                SubDataType = message.SubDataType,
                StartTime = message.StartTime,
                EndTime = message.EndTime
            }
        );
    }

    private void SendUnsubscribeRequest(
        PxHistoryRequestIdentifier identifier,
        PxHistoryRequestParams @params
    ) {
        SubscribedRequests.Remove(identifier);
        Client.RequestSocket.SendTcRequest<PxHistoryUnsubscribeRequest, PxHistoryUnsubscribeReply>(
            new PxHistoryUnsubscribeRequest { SessionKey = Client.SessionKey, Param = @params }
        );
    }

    private PxHistoryDataReply GetPartialData(PxHistoryReadyMessage message, int queryIndex) {
        return Client.RequestSocket.SendTcRequest<PxHistoryDataRequest, PxHistoryDataReply>(
            new PxHistoryDataRequest {
                SessionKey = Client.SessionKey,
                Param = new PxHistoryDataRequestParams {
                    Symbol = message.Symbol,
                    SubDataType = message.SubDataType,
                    StartTime = message.StartTime,
                    EndTime = message.EndTime,
                    QryIndex = queryIndex
                }
            },
            true
        );
    }

    public HistoryEventArgs? GetHistoryData(PxHistoryReadyMessage message, CancellationToken cancellationToken) {
        if (!message.IsReady) {
            Log.Warning("[{Identifier}] History data not ready ({Status})", message.IdentifierString, message.Status);
            return null;
        }

        if (!SubscribedRequests.TryGetValue(message.Identifier, out var isSubscription)) {
            Log.Warning("[{Identifier}] Skipped processing unrequested history data", message.IdentifierString);
            return null;
        }

        var queryIndex = 0;
        var historyDataFetched = new Dictionary<DateTime, PxHistoryEntry>();

        Log.Information("[{Identifier}] Start processing history data", message.IdentifierString);

        while (!cancellationToken.IsCancellationRequested) {
            var historyData = GetPartialData(message, queryIndex);
            if (historyData.HisData.IsEmpty()) {
                break;
            }

            foreach (var data in historyData.HisData) {
                historyDataFetched[data.Timestamp] = data;
            }

            queryIndex = historyData.LastQueryIndex;

            if (queryIndex > 0 && queryIndex % 5000 == 0) {
                Log.Information(
                    "[{Identifier}] Received {Count} history data",
                    message.IdentifierString,
                    queryIndex
                );
            }
        }

        if (historyDataFetched.Count > 0) {
            Log.Information(
                "[{Identifier}] Received {Count} history data in total",
                message.IdentifierString,
                historyDataFetched.Count
            );
        } else {
            Log.Error("[{Identifier}] No history data available", message.IdentifierString);
        }

        // If not subscribed
        // ReSharper disable once InvertIf
        if (SubscribedRequests.TryGetValue(message.Identifier, out var subscribed) && !subscribed) {
            SendUnsubscribeRequest(message);

            Log.Information("[{Identifier}] Completed history data request", message.IdentifierString);
        }

        return new HistoryEventArgs {
            Metadata = message with {
                Symbol = PxConfigController.GetInternalSymbol(message.Symbol, PxSource.Touchance)
            },
            Data = historyDataFetched
                .Select(r => r.Value)
                .ToArray<IHistoryDataEntry>(),
            IsSubscription = isSubscription
        };
    }
}