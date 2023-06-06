using Grpc.Net.Client;
using KL.Common.Extensions;
using KL.Common.Utils;
using KL.Proto;

namespace KL.GrpcCaller.Grpc;


using ILogger = Serilog.ILogger;

public class GrpcPxInfoCaller {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(GrpcPxInfoCaller));

    private static readonly GrpcClientWrapper<PxInfo.PxInfoClient> ClientWrapper = new(
        () => new PxInfo.PxInfoClient(
            GrpcChannel.ForAddress($"http://localhost:{EnvironmentConfigHelper.Config.Grpc.PxInfoPort}")
        )
    );

    public static Task<bool> GetOptionsOi(DateOnly date, string symbol, CancellationToken cancellationToken) {
        const string endpointName = nameof(ClientWrapper.Client.GetOptionsOi);
        var request = new PxInfoGetOptionsOiOptions {
            Symbol = symbol,
            Date = date.ToGrpcDate()
        };

        return GrpcHelper.ServerStream(
            ClientWrapper,
            ClientWrapper.Client.GetOptionsOi,
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
            cancellationToken
        );
    }

    public static Task<bool> GetFinancialEvents(DateOnly date, CancellationToken cancellationToken) {
        const string endpointName = nameof(ClientWrapper.Client.GetFinancialEvents);
        var request = new PxInfoGetFinancialEventsOptions {
            Date = date.ToGrpcDate()
        };

        return GrpcHelper.ServerStream(
            ClientWrapper,
            ClientWrapper.Client.GetFinancialEvents,
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
            cancellationToken
        );
    }
}