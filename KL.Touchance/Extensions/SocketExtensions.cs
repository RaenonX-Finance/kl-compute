using KL.Common.Utils;
using KL.Touchance.Requests;
using KL.Touchance.Responses;
using KL.Touchance.Utils;
using NetMQ;
using NetMQ.Sockets;
using Serilog;

namespace KL.Touchance.Extensions;


public static class SocketExtensions {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(SocketExtensions));

    public static TReply SendTcRequest<TRequest, TReply>(
        this RequestSocket socket,
        TRequest request,
        bool hasPrefix = false
    )
        where TRequest : TcRequest
        where TReply : TcReply {
        var timeout = EnvironmentConfigHelper.Config.Source.Touchance.Timeout.Request;
        var response = socket.SendTcRequest<TRequest, TReply>(
            request,
            new TimeSpan(0, 0, 0, timeout),
            hasPrefix
        );

        if (response is not null) {
            return response;
        }

        var exception = new TimeoutException($"Request ({request.ToJson()}) timed out after {timeout} seconds");

        Log.Error(
            exception,
            "Request {Type} timed out after {TimeoutSec} seconds",
            request.GetType(),
            timeout
        );

        throw exception;
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