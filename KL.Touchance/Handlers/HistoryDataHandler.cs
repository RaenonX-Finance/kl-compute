﻿using KL.Common.Controllers;
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


internal class HistoryDataHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataHandler));

    // Has key = request to handle; value is true = is history data subscription
    private readonly IDictionary<PxHistoryRequestIdentifier, bool> _subscribedRequests
        = new Dictionary<PxHistoryRequestIdentifier, bool>();

    internal required TouchanceClient Client { get; init; }

    internal async Task SendHandshakeRequest(
        string touchanceSymbol,
        HistoryInterval interval,
        DateTime start,
        DateTime end,
        bool isSubscription
    ) {
        Log.Information(
            "Sending history data handshake to {Action} {Symbol} @ {Interval}: {Start} ~ {End}",
            isSubscription ? "subscribe" : "request",
            touchanceSymbol,
            interval,
            start,
            end
        );

        var dataRange = await HistoryDataController.GetStoredDataRange(
            PxConfigController.GetInternalSymbol(touchanceSymbol, PxSource.Touchance),
            interval
        );

        if (start < dataRange?.Start) {
            Log.Information(
                "Missing history of {Symbol} @ {Interval} from {Start} to {End}",
                touchanceSymbol,
                interval,
                start,
                dataRange.Value.Start
            );
            SendHandshakeRequest(
                new PxHistoryRequestIdentifier {
                    Symbol = touchanceSymbol,
                    Interval = interval,
                    Start = start,
                    // Add additional 1 hour to guarantee data range overlap
                    End = dataRange.Value.Start.AddHours(1)
                },
                isSubscription
            );
        }

        if (end > dataRange?.End) {
            Log.Information(
                "Missing history of {Symbol} @ {Interval} from {Start} to {End}",
                touchanceSymbol,
                interval,
                dataRange.Value.End.AddHours(-1),
                end
            );
            SendHandshakeRequest(
                new PxHistoryRequestIdentifier {
                    Symbol = touchanceSymbol,
                    Interval = interval,
                    // Move back additional 1 hour to guarantee data range overlap
                    Start = dataRange.Value.End.AddHours(-1),
                    End = end
                },
                isSubscription
            );
        }
    }

    private void SendHandshakeRequest(PxHistoryRequestIdentifier identifier, bool isSubscription) {
        // Unsubscribe first to ensure successful subscription
        SendUnsubscribeRequest(identifier);

        Log.Information(
            "[{Identifier}] {Action} history data",
            ((IHistoryMetadata)identifier).ToIdentifier(),
            isSubscription ? "Subscribing" : "Requesting"
        );
        _subscribedRequests[identifier] = isSubscription;
        Client.RequestSocket.SendTcRequest<PxHistoryHandshakeRequest, PxSubscribedReply>(
            new PxHistoryHandshakeRequest { SessionKey = Client.SessionKey, Param = identifier.ToHandshakeParams() }
        );
    }

    private void SendUnsubscribeRequest(PxHistoryRequestIdentifier identifier) {
        // Print the log only if `identifier` is really subscribed
        if (_subscribedRequests.Remove(identifier)) {
            Log.Information(
                "[{Identifier}] Unsubscribing history data",
                ((IHistoryMetadata)identifier).ToIdentifier()
            );
        }

        // Still sends `PxHistoryUnsubscribeRequest`
        // because the request of `identifier` could be initiated by other run
        Client.RequestSocket.SendTcRequest<PxHistoryUnsubscribeRequest, PxUnsubscribedReply>(
            new PxHistoryUnsubscribeRequest { SessionKey = Client.SessionKey, Param = identifier.ToHandshakeParams() }
        );
    }

    private PxHistoryDataReply GetPartialData(PxHistoryReadyMessage message, int queryIndex) {
        return Client.RequestSocket.SendTcRequest<PxHistoryDataRequest, PxHistoryDataReply>(
            new PxHistoryDataRequest {
                SessionKey = Client.SessionKey,
                Param = new PxHistoryDataRequestParams {
                    Symbol = message.Symbol,
                    Interval = message.Interval,
                    StartTime = message.StartTime,
                    EndTime = message.EndTime,
                    QryIndex = queryIndex
                }
            },
            true
        );
    }

    internal HistoryEventArgs? GetHistoryData(PxHistoryReadyMessage message, CancellationToken cancellationToken) {
        if (!message.IsReady) {
            Log.Warning("[{Identifier}] History data not ready ({Status})", message.IdentifierString, message.Status);
            return null;
        }

        if (!_subscribedRequests.TryGetValue(message.Identifier, out var isSubscription)) {
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
        if (_subscribedRequests.TryGetValue(message.Identifier, out var subscribed) && !subscribed) {
            SendUnsubscribeRequest(message.Identifier);

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