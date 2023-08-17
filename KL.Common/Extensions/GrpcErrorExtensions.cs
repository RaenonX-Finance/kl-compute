using Grpc.Core;

namespace KL.Common.Extensions;


public static class GrpcErrorExtensions {
    private const string MessageResponseEnded
        = "The response ended prematurely while waiting for the next frame from the server.";

    private const string MessageStreamReset = "The HTTP/2 server reset the stream.";

    public static bool IsServerClosed(this RpcException exception) {
        var message = exception.Message;

        return message.Contains(MessageResponseEnded) || message.Contains(MessageStreamReset);
    }
}