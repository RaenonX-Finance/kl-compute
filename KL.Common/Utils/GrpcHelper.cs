using Grpc.Core;
using KL.Common.Extensions;
using Serilog;

namespace KL.Common.Utils;


public static class GrpcHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcHelper));

    private static readonly ChronoGate<string> ChronoGate = new();

    private const int MaxUnavailableRetryCount = 5;

    public delegate AsyncUnaryCall<TReply> GrpcUnaryCall<in TRequest, TReply>(
        TRequest request,
        Metadata? metadata = null,
        DateTime? deadline = null,
        CancellationToken cancellationToken = default
    );

    public static Task<TReply?> CallWithDeadline<TClient, TRequest, TReply>(
        GrpcClientWrapper<TClient> grpcClient,
        GrpcUnaryCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0,
        string reason = "(Unknown)"
    ) where TClient : ClientBase<TClient> {
        return CallWithDeadline(
            grpcClient,
            grpcCall,
            request,
            endpointName,
            false,
            cancellationToken,
            useTimeout: useTimeout,
            timeoutExtension: timeoutExtension,
            reason: reason
        );
    }

    private static async Task<TReply?> CallWithDeadline<TClient, TRequest, TReply>(
        GrpcClientWrapper<TClient> grpcClient,
        GrpcUnaryCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        bool isFireAndForget,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0,
        string reason = "(Unknown)",
        int retryCount = 1
    ) where TClient : ClientBase<TClient> {
        Log.Information(
            "Calling gRPC unary `{GrpcCallEndpoint}` {GrpcCallType} ({Reason})",
            endpointName,
            isFireAndForget ? "in fire and forget" : "asynchronously",
            reason
        );

        var timeout = EnvironmentConfigHelper.Config.Grpc.Timeout.Default + timeoutExtension;

        if (useTimeout && !ChronoGate.IsGateOpened(endpointName, timeout, out var nextOpen)) {
            Log.Warning(
                "`{GrpcCallEndpoint}` still in cooldown, next available call at {GrpcGateAllowTime}",
                endpointName,
                nextOpen
            );
            return default;
        }

        try {
            return await grpcCall(
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
                    return default;
                case { StatusCode: StatusCode.Internal }:
                case { StatusCode: StatusCode.Unavailable }:
                    if (retryCount < MaxUnavailableRetryCount) {
                        if (!e.IsServerClosed()) {
                            Log.Warning(
                                e,
                                "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody}), #{RetryCount} attempt to reconnect in {RetryWaitSec}s",
                                endpointName,
                                request,
                                retryCount,
                                retryCount
                            );
                        }

                        await Task.Delay(retryCount * 1000, cancellationToken);
                        grpcClient.Reconnect();

                        return await CallWithDeadline(
                            grpcClient,
                            grpcCall,
                            request,
                            endpointName,
                            isFireAndForget,
                            cancellationToken,
                            useTimeout,
                            timeoutExtension,
                            reason,
                            retryCount: retryCount + 1
                        );
                    }

                    if (!e.IsServerClosed()) {
                        Log.Error(
                            e,
                            "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody}), max retry exceeded",
                            endpointName,
                            request
                        );
                    }
                    return default;
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

    public static void CallWithDeadlineAsync<TClient, TRequest, TReply>(
        GrpcClientWrapper<TClient> grpcClient,
        GrpcUnaryCall<TRequest, TReply> grpcCall,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool useTimeout = false,
        int timeoutExtension = 0,
        string reason = "(Unknown)"
    ) where TClient : ClientBase<TClient> {
        // Cannot use `nameof(grpcRequestFunc)` because it would return literally `grpcRequestFunc`
        TaskHelper.FireAndForget(
            () => CallWithDeadline(
                grpcClient,
                grpcCall,
                request,
                endpointName,
                cancellationToken,
                useTimeout: useTimeout,
                timeoutExtension: timeoutExtension,
                reason: reason
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

    public delegate Task OnGrpcStreamReply<in TReply>(TReply reply);

    public static async Task<bool> ServerStream<TClient, TRequest, TReply>(
        GrpcClientWrapper<TClient> grpcClient,
        GrpcServerStreamCall<TRequest, TReply> grpcCall,
        OnGrpcStreamReply<TReply> onReply,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken,
        bool useTimeout = true,
        int timeoutExtension = 0,
        int retryCount = 1
    ) where TClient : ClientBase<TClient> {
        Log.Information("Calling gRPC server stream `{GrpcCallEndpoint}`", endpointName);

        var timeout = EnvironmentConfigHelper.Config.Grpc.Timeout.Default + timeoutExtension;

        if (useTimeout && !ChronoGate.IsGateOpened(endpointName, timeout, out var nextOpen)) {
            Log.Warning(
                "`{GrpcCallEndpoint}` still in cooldown, next available call at {GrpcGateAllowTime}",
                endpointName,
                nextOpen
            );
            return false;
        }

        try {
            using var streamingCall = grpcCall(
                request,
                new CallOptions(
                    deadline: useTimeout ? DateTime.UtcNow.AddMilliseconds(timeout) : null,
                    cancellationToken: cancellationToken
                )
            );

            await foreach (
                var reply in streamingCall.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken)
            ) {
                await onReply(reply);
            }

            return true;
        } catch (RpcException e) {
            switch (e) {
                case { StatusCode: StatusCode.DeadlineExceeded }:
                    Log.Warning(
                        "Server streaming call to gRPC `{GrpcCallEndpoint}` deadline exceeded (Timeout: {Timeout} ms)",
                        endpointName,
                        timeout
                    );
                    return false;
                case { StatusCode: StatusCode.Internal }:
                case { StatusCode: StatusCode.Unavailable }:
                    if (retryCount < MaxUnavailableRetryCount) {
                        if (!e.IsServerClosed()) {
                            Log.Warning(
                                e,
                                "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody}), #{RetryCount} attempt to reconnect in {RetryWaitSec}s",
                                endpointName,
                                request,
                                retryCount,
                                retryCount
                            );
                        }

                        await Task.Delay(retryCount * 1000, cancellationToken);
                        grpcClient.Reconnect();

                        return await ServerStream(
                            grpcClient,
                            grpcCall,
                            onReply,
                            request,
                            endpointName,
                            cancellationToken,
                            useTimeout: true,
                            timeoutExtension: 0,
                            retryCount: retryCount + 1
                        );
                    }

                    if (!e.IsServerClosed()) {
                        Log.Error(
                            e,
                            "gRPC server unavailable for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody}), max retry exceeded",
                            endpointName,
                            request
                        );
                    }
                    return false;
                case { StatusCode: StatusCode.Cancelled }:
                    Log.Warning(
                        e,
                        "gRPC server stream call cancelled for `{GrpcCallEndpoint}` (Call body: {@GrpcCallBody})",
                        endpointName,
                        request
                    );
                    return false;
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