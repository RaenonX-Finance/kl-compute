using Grpc.Net.Client;
using KL.Common.Utils;
using KL.PxParse;
using ILogger = Serilog.ILogger;

namespace KL.GrpcCaller.Grpc;


public class GrpcPxParseCaller {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcPxParseCaller));

    private static readonly GrpcClientWrapper<PxParse.PxParse.PxParseClient> ClientWrapper = new(
        () => new PxParse.PxParse.PxParseClient(
            GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.PxParsePort}")
        )
    );

    public static Task<bool> Subscribe(IEnumerable<string> symbols, CancellationToken cancellationToken) {
        const string endpointName = nameof(ClientWrapper.Client.Subscribe);
        var request = new PxParseSubscribe();

        request.Symbols.AddRange(symbols);

        return GrpcHelper.ServerStream(
            ClientWrapper,
            ClientWrapper.Client.Subscribe,
            reply => Task.Run(
                () => Log.Information(
                    "Stream message of {GrpcCallEndpoint}: {Message}",
                    endpointName,
                    reply.Message
                ),
                cancellationToken
            ),
            request,
            endpointName,
            cancellationToken,
            useTimeout: false,
            // Add additional 30s on top of existing timeout because `CalcAll` should take longer to calculate 
            timeoutExtension: EnvironmentConfigHelper.Config.Grpc.Timeout.CalcAll
        );
    }
}