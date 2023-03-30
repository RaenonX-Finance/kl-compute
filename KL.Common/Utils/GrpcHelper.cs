using Grpc.Core;
using Serilog;

namespace KL.Common.Utils;


public static class GrpcHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcHelper));

    private static readonly ChronoGate<string> ChronoGate = new();

    public delegate AsyncUnaryCall<TReply> GrpcUnaryCall<in TRequest, TReply>(
        TRequest request,
        Metadata? metadata = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default
    );

    public static Task CallWithDeadline<TRequest, TReply>(
        GrpcUnaryCall<TRequest, TReply> grpcCall,
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
        GrpcUnaryCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        bool isFireAndForget,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0
    ) {
        Log.Information(
            "Calling gRPC unary `{GrpcCallEndpoint}` {GrpcCallType}",
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
            switch (e) {
                case { StatusCode: StatusCode.DeadlineExceeded }:
                    Log.Warning(
                        "Unary call to gRPC `{GrpcCallEndpoint}` deadline exceeded (Timeout: {Timeout} ms)",
                        endpointName,
                        timeout
                    );
                    return;
                case { StatusCode: StatusCode.Unavailable }:
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
        GrpcUnaryCall<TRequest, TReply> grpcCall,
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

    public delegate AsyncServerStreamingCall<TReply> GrpcServerStreamCall<in TRequest, TReply>(
        TRequest request,
        CallOptions options
    );
    
    public delegate void OnGrpcStreamReply<in TReply>(TReply reply);

    public static async Task ServerStream<TRequest, TReply>(
        GrpcServerStreamCall<TRequest, TReply> grpcCall,
        OnGrpcStreamReply<TReply> onReply,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0
    ) {
        Log.Information("Calling gRPC server stream `{GrpcCallEndpoint}`", endpointName);

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
            using var streamingCall = grpcCall(
                request,
                new CallOptions(
                    deadline: useTimeout ? DateTime.UtcNow.AddMilliseconds(timeout) : null,
                    cancellationToken: cancellationToken
                )
            );
            
            await foreach (var reply in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken)) {
                onReply(reply);
            }
        } catch (RpcException e) {
            switch (e) {
                case { StatusCode: StatusCode.DeadlineExceeded }:
                    Log.Warning(
                        "Server streaming call to gRPC `{GrpcCallEndpoint}` deadline exceeded (Timeout: {Timeout} ms)",
                        endpointName,
                        timeout
                    );
                    return;
                case { StatusCode: StatusCode.Unavailable }:
                    Log.Warning(
                        e,
                        "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody})",
                        endpointName,
                        request
                    );
                    return;
                case { StatusCode: StatusCode.Cancelled }:
                    Log.Warning(
                        e,
                        "gRPC server stream call cancelled for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody})",
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
}