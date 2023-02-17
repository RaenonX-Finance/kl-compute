using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using KL.Common.Extensions;
using KL.Common.Utils;
using KL.Proto;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Utils;


public static class GrpcHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcHelper));

    private static readonly PxData.PxDataClient PxDataClient
        = new(GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.CalcPort}"));

    private static void CallGrpcAsyncRequest<TRequest>(
        Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<PxCalcReply>> grpcRequestFunc,
        TRequest request,
        string endpointName,
        CancellationToken cancellationToken
    ) {
        Log.Information(
            "Sending gRPC request to {Endpoint} with fire and forget (Request Body: {@RequestBody})",
            endpointName,
            request
        );

        TaskHelper.FireAndForget(
            async () => await grpcRequestFunc(request, null, null, cancellationToken),
            exception => Log.Error(exception, "Error on gRPC endpoint: {Endpoint}", endpointName),
            cancellationToken
        );
    }

    public static void CalcLastAsync(string symbol, CancellationToken cancellationToken) {
        CallGrpcAsyncRequest(
            PxDataClient.CalcLastAsync,
            new PxCalcRequestSingle { Symbol = symbol },
            nameof(PxDataClient.CalcLastAsync),
            cancellationToken
        );
    }

    public static void CalcPartialAsync(IEnumerable<string> symbols, CancellationToken cancellationToken) {
        var request = new PxCalcRequestMulti();
        request.Symbols.AddRange(symbols);

        CallGrpcAsyncRequest(
            PxDataClient.CalcPartialAsync,
            request,
            nameof(PxDataClient.CalcPartialAsync),
            cancellationToken
        );
    }

    public static async Task CalcAll(IEnumerable<string> symbols, CancellationToken cancellationToken) {
        var start = Stopwatch.GetTimestamp();
        var request = new PxCalcRequestMulti();
        request.Symbols.AddRange(symbols);

        Log.Information("Sending gRPC request to {Endpoint}", nameof(PxDataClient.CalcAllAsync));

        try { 
            await PxDataClient.CalcAllAsync(request, null, null, cancellationToken);
        } catch (Exception e) {
            Log.Error(e, "Exception occurred during gRPC CalcAll request");
            throw;
        }

        Log.Information(
            "gRPC request of {Endpoint} completed in {ElapsedMs:0.00} ms",
            nameof(PxDataClient.CalcAllAsync),
            start.GetElapsedMs()
        );
    }
}