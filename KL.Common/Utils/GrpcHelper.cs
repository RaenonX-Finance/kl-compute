using Grpc.Core;
using Serilog;

namespace KL.Common.Utils;


public static class GrpcHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcHelper));

    private static readonly ChronoGate<string> ChronoGate = new();

    public delegate AsyncUnaryCall<TReply> GrpcCall<in TRequest, TReply>(
        TRequest request,
        Metadata? metadata = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default
    );

    public static Task CallWithDeadline<TRequest, TReply>(
        GrpcCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        int timeoutExtension = 0
    ) {
        return CallWithDeadline(
            grpcCall,
            request,
            endpointName,
            cancellationToken,
            isFireAndForget: false,
            timeoutExtension: timeoutExtension
        );
    }

    private static async Task CallWithDeadline<TRequest, TReply>(
        GrpcCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool isFireAndForget,
        int timeoutExtension = 0
    ) {
        Log.Information(
            "Calling gRPC `{GrpcCallEndpoint}` {GrpcCallType} (Request Body: {@GrpcCallBody})",
            endpointName,
            isFireAndForget ? "in fire and forget" : "asynchronously",
            request
        );

        var timeout = EnvironmentConfigHelper.Config.Grpc.Timeout.Default + timeoutExtension;

        if (!ChronoGate.IsGateOpened(endpointName, timeout, out var nextOpen)) {
            Log.Warning(
                "`{GrpcCallEndpoint}` still in cooldown, next available call at {GrpcGateAllowTime}",
                endpointName,
                nextOpen
            );
            return;
        }

        try {
            await grpcCall(
                request,
                deadline: DateTime.UtcNow.AddMilliseconds(timeout),
                cancellationToken: cancellationToken
            );
        } catch (RpcException e) {
            if (e.StatusCode != StatusCode.DeadlineExceeded) {
                Log.Error(e, "Error on gRPC call to `{GrpcCallEndpoint}`", endpointName);
                throw;
            }

            Log.Warning(
                "Call to gRPC `{GrpcCallEndpoint}` deadline exceeded (Timeout: {Timeout} ms)",
                endpointName,
                timeout
            );
        }
    }

    public static void CallWithDeadlineAsync<TRequest, TReply>(
        GrpcCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        int timeoutExtension = 0
    ) {
        // Cannot use `nameof(grpcRequestFunc)` because it would return literally `grpcRequestFunc`
        TaskHelper.FireAndForget(
            () => CallWithDeadline(
                grpcCall,
                request,
                endpointName,
                cancellationToken,
                timeoutExtension: timeoutExtension
            ),
            exception => {
                if (exception != null) {
                    throw exception;
                }

                Log.Error("`null` exception thrown in gRPC call to `{GrpcCallEndpoint}`", endpointName);
                throw new InvalidOperationException($"`null` exception thrown in gRPC call to `{endpointName}`");
            },
            cancellationToken
        );
    }
}