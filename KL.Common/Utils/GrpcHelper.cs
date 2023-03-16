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
        bool useTimeout = true,
        int timeoutExtension = 0
    ) {
        return CallWithDeadline(
            grpcCall,
            request,
            endpointName,
            false,
            cancellationToken,
            useTimeout: useTimeout,
            timeoutExtension: timeoutExtension
        );
    }

    private static async Task CallWithDeadline<TRequest, TReply>(
        GrpcCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        bool isFireAndForget,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0
    ) {
        Log.Information(
            "Calling gRPC `{GrpcCallEndpoint}` {GrpcCallType}",
            endpointName,
            isFireAndForget ? "in fire and forget" : "asynchronously"
        );

        var timeout = EnvironmentConfigHelper.Config.Grpc.Timeout.Default + timeoutExtension;

        if (useTimeout && !ChronoGate.IsGateOpened(endpointName, timeout, out var nextOpen)) {
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
                deadline: useTimeout ? DateTime.UtcNow.AddMilliseconds(timeout) : null,
                cancellationToken: cancellationToken
            );
        } catch (RpcException e) {
            switch (e.StatusCode) {
                case StatusCode.DeadlineExceeded:
                    Log.Warning(
                        "Call to gRPC `{GrpcCallEndpoint}` deadline exceeded (Timeout: {Timeout} ms)",
                        endpointName,
                        timeout
                    );
                    return;
                case StatusCode.Unavailable:
                    Log.Warning(
                        e,
                        "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody})",
                        endpointName,
                        request
                    );
                    return;
                default:
                    Log.Error(
                        e,
                        "Error on gRPC call to `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody})",
                        endpointName,
                        request
                    );
                    throw;
            }
        }
    }

    public static void CallWithDeadlineAsync<TRequest, TReply>(
        GrpcCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool useTimeout = false,
        int timeoutExtension = 0
    ) {
        // Cannot use `nameof(grpcRequestFunc)` because it would return literally `grpcRequestFunc`
        TaskHelper.FireAndForget(
            () => CallWithDeadline(
                grpcCall,
                request,
                endpointName,
                cancellationToken,
                useTimeout: useTimeout,
                timeoutExtension: timeoutExtension
            ),
            exception => {
                if (exception is not null) {
                    throw exception;
                }

                Log.Error("`null` exception thrown in gRPC call to `{GrpcCallEndpoint}`", endpointName);
                throw new InvalidOperationException($"`null` exception thrown in gRPC call to `{endpointName}`");
            },
            cancellationToken
        );
    }
}