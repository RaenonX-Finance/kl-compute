using System.Diagnostics;
using Grpc.Net.Client;
using KL.Common.Events;
using KL.Common.Extensions;
using KL.Common.Utils;
using KL.Proto;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Grpc;


public class GrpcPxDataCaller {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcPxDataCaller));

    private static readonly GrpcClientWrapper<PxData.PxDataClient> ClientWrapper = new(
        () => new PxData.PxDataClient(
            GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.CalcPort}")
        )
    );

    public static void CalcLastAsync(string symbol, CancellationToken cancellationToken) {
        GrpcHelper.CallWithDeadlineAsync(
            ClientWrapper,
            ClientWrapper.Client.CalcLastAsync,
            new PxCalcRequestSingle { Symbol = symbol },
            nameof(ClientWrapper.Client.CalcLastAsync),
            cancellationToken,
            reason: $"{symbol} received realtime data (GrpcPxData)"
        );
    }

    public static Task CalcPartial(string symbol, CancellationToken cancellationToken) {
        const string endpointName = nameof(ClientWrapper.Client.CalcPartialAsync);

        var request = new PxCalcRequestMulti();
        request.Symbols.Add(symbol);

        return GrpcHelper.CallWithDeadline(
            ClientWrapper,
            ClientWrapper.Client.CalcPartialAsync,
            request,
            endpointName,
            cancellationToken,
            useTimeout: false,
            reason: $"{symbol} minute changed"
        );
    }

    public static async Task CalcAll(InitCompletedEventArgs e, CancellationToken cancellationToken) {
        var start = Stopwatch.GetTimestamp();
        const string endpointName = nameof(ClientWrapper.Client.CalcAll);

        var request = new PxCalcRequestMulti();
        request.Symbols.AddRange(e.Sources.Select(r => r.InternalSymbol));

        if (request.Symbols.IsEmpty()) {
            Log.Error("gRPC call {GrpcCallEndpoint} should have symbols for calculation", endpointName);
            await e.OnUpdate($"gRPC call {endpointName} should have symbols for calculation");
            return;
        }

        await GrpcHelper.ServerStream(
            ClientWrapper,
            ClientWrapper.Client.CalcAll,
            reply => Task.WhenAll(
                e.OnUpdate(reply.Message),
                Task.Run(
                    () => Log.Information(
                        "gRPC stream message of {GrpcCallEndpoint}: {Message}",
                        endpointName,
                        reply.Message
                    ),
                    cancellationToken
                )
            ),
            request,
            endpointName,
            cancellationToken,
            useTimeout: false,
            // Add additional 30s on top of existing timeout because `CalcAll` should take longer to calculate 
            timeoutExtension: EnvironmentConfigHelper.Config.Grpc.Timeout.CalcAll
        );

        Log.Information(
            "gRPC request of {GrpcCallEndpoint} completed in {Elapsed:0.00} ms",
            endpointName,
            start.GetElapsedMs()
        );
    }
}