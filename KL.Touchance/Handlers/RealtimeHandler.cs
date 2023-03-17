using KL.Touchance.Extensions;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using Serilog;

namespace KL.Touchance.Handlers;


internal class RealtimeHandler {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(RealtimeHandler));

    internal required TouchanceClient Client { get; init; }

    private readonly ISet<string> _subscribedSymbols = new HashSet<string>();

    internal void SubscribeRealtime(string touchanceSymbol) {
        // Unsubscribe first to ensure successful subscription
        UnsubscribeRealtime(touchanceSymbol);
        
        Log.Information("Subscribing realtime data of {Symbol}", touchanceSymbol);

        _subscribedSymbols.Add(touchanceSymbol);
        Client.RequestSocket.SendTcRequest<PxRealtimeSubscribeRequest, PxSubscribedReply>(
            new PxRealtimeSubscribeRequest {
                SessionKey = Client.SessionKey, Param = new PxRealtimeRequestParams {
                    Symbol = touchanceSymbol
                }
            }
        );
    }

    private void UnsubscribeRealtime(string touchanceSymbol) {
        // Print the log only if `touchanceSymbol` is really subscribed
        if (_subscribedSymbols.Remove(touchanceSymbol)) {
            Log.Information("Unsubscribing realtime data of {Symbol}", touchanceSymbol);
        }

        // Still sends `PxRealtimeUnsubscribeRequest`
        // because the request of `touchanceSymbol` could be initiated by other run
        Client.RequestSocket.SendTcRequest<PxRealtimeUnsubscribeRequest, PxUnsubscribedReply>(
            new PxRealtimeUnsubscribeRequest {
                SessionKey = Client.SessionKey, Param = new PxRealtimeRequestParams {
                    Symbol = touchanceSymbol
                }
            }
        );
    }

    internal bool IsSubscribing(string touchanceSymbol) {
        return _subscribedSymbols.Contains(touchanceSymbol);
    }
}