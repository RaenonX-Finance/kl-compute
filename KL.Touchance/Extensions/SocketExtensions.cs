using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Utils;
using NetMQ;
using NetMQ.Sockets;

namespace KL.Touchance.Extensions;


public static class SocketExtensions {
    public static TReply SendTcRequest<TRequest, TReply>(
        this RequestSocket socket,
        TRequest request,
        bool hasPrefix = false
    )
        where TRequest : TcRequest
        where TReply : TcReply {
        socket.SendFrame(request.ToJson());

        return ParseReplyToJson<TReply>(socket.ReceiveFrameString(), hasPrefix);
    }

    public static TReply? SendTcRequest<TRequest, TReply>(
        this RequestSocket socket,
        TRequest request,
        TimeSpan timeout,
        bool hasPrefix = false
    )
        where TRequest : TcRequest
        where TReply : TcReply {
        socket.SendFrame(request.ToJson());

        var hasMessage = socket.TryReceiveFrameString(timeout, out var response);

        if (!hasMessage || response is null) {
            return null;
        }

        return ParseReplyToJson<TReply>(response, hasPrefix);
    }

    private static TReply ParseReplyToJson<TReply>(
        string response,
        bool hasPrefix = false
    ) where TReply : TcReply {
        if (hasPrefix) {
            // Only care about the message after 1st colon
            response = response.Split(":", 2)[1];
        }

        return response.Deserialize<TReply>();
    }
}