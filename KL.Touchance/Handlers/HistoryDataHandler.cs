using System.Collections.Immutable;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Interfaces;
using KL.Touchance.Extensions;
using KL.Touchance.Models;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Subscriptions;
using Serilog;

namespace KL.Touchance.Handlers;


public static class HistoryDataHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(HistoryDataHandler));

    // Has key = request to handle
    // Value is true = is history data subscription
    private static readonly IDictionary<PxHistoryRequestIdentifier, bool> SubscribedRequests
        = new Dictionary<PxHistoryRequestIdentifier, bool>();

    internal static void SendHandshakeRequest(
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

        TouchanceClient.RequestSocket.SendTcRequest<PxHistoryHandshakeRequest, PxHistoryHandshakeReply>(
            new PxHistoryHandshakeRequest {
                SessionKey = TouchanceClient.SessionKey,
                Param = new PxHistoryHandshakeRequestParams {
                    Symbol = touchanceSymbol,
                    SubDataType = interval.GetTouchanceType(),
                    StartTime = startTime,
                    EndTime = endTime
                }
            }
        );

        var identifier = new PxHistoryRequestIdentifier {
            Symbol = touchanceSymbol,
            Interval = interval,
            Start = startTime.FromTouchanceHourlyPrecision(),
            End = endTime.FromTouchanceHourlyPrecision()
        };

        SubscribedRequests.Add(identifier, isSubscription);

        Log.Information(
            "[{Identifier}] {Action} history data",
            ((IHistoryMetadata)identifier).ToIdentifier(),
        isSubscription ? "Subscribing" : "Requesting"
        );
    }

    private static void SendUnsubscribeRequest(PxHistoryReadyMessage message) {
        SubscribedRequests.Remove(message.Identifier);

        TouchanceClient.RequestSocket.SendTcRequest<PxHistoryUnsubscribeRequest, PxHistoryUnsubscribeReply>(
            new PxHistoryUnsubscribeRequest {
                SessionKey = TouchanceClient.SessionKey,
                Param = new PxHistoryHandshakeRequestParams {
                    Symbol = message.Symbol,
                    SubDataType = message.SubDataType,
                    StartTime = message.StartTime,
                    EndTime = message.EndTime
                }
            }
        );
    }

    private static PxHistoryDataReply GetPartialData(PxHistoryReadyMessage message, int queryIndex) {
        return TouchanceClient.RequestSocket.SendTcRequest<PxHistoryDataRequest, PxHistoryDataReply>(
            new PxHistoryDataRequest {
                SessionKey = TouchanceClient.SessionKey,
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

    public static HistoryEventArgs? GetHistoryData(PxHistoryReadyMessage message, CancellationToken cancellationToken) {
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
            if (historyData.HisData.Length == 0) {
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
                .ToImmutableArray<IHistoryDataEntry>(),
            IsSubscription = isSubscription
        };
    }
}