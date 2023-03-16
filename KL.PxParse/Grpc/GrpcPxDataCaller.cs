using System.Diagnostics;
using Grpc.Net.Client;
using KL.Common.Extensions;
using KL.Common.Utils;
using KL.Proto;
using ILogger = Serilog.ILogger;

namespace KL.PxParse.Grpc;


public class GrpcPxDataCaller {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcPxDataCaller));

    private static readonly PxData.PxDataClient Client
        = new(GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.CalcPort}"));

    public static void CalcLastAsync(string symbol, CancellationToken cancellationToken) {
        GrpcHelper.CallWithDeadlineAsync(
            Client.CalcLastAsync,
            new PxCalcRequestSingle { Symbol = symbol },
            nameof(Client.CalcLastAsync),
            cancellationToken
        );
    }

    public static Task CalcPartial(string symbol, CancellationToken cancellationToken) {
        const string endpointName = nameof(Client.CalcPartialAsync);

        var request = new PxCalcRequestMulti();
        request.Symbols.Add(symbol);

        return GrpcHelper.CallWithDeadline(
            Client.CalcPartialAsync,
            request,
            endpointName,
            cancellationToken,
            useTimeout: false
        );
    }

    public static async Task CalcAll(IEnumerable<string> symbols, CancellationToken cancellationToken) {
        var start = Stopwatch.GetTimestamp();

        const string endpointName = nameof(Client.CalcAllAsync);

        var request = new PxCalcRequestMulti();
        request.Symbols.AddRange(symbols);

        if (request.Symbols.IsEmpty()) {
            Log.Error("gRPC call {GrpcCallEndpoint} should have symbols for calculation", endpointName);
            return;
        }

        await GrpcHelper.CallWithDeadline(
            Client.CalcAllAsync,
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