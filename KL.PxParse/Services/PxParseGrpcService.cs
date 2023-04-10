using Grpc.Core;
using KL.PxParse.Interfaces;

namespace KL.PxParse.Services;


public class PxParseGrpcService : PxParse.PxParseBase {
    private readonly ILogger<PxParseGrpcService> _logger;

    private readonly IClientAggregator _clientAggregator;

    public PxParseGrpcService(ILogger<PxParseGrpcService> logger, IClientAggregator clientAggregator) {
        _logger = logger;
        _clientAggregator = clientAggregator;
    }

    public override async Task Subscribe(
        PxParseSubscribe request,
        IServerStreamWriter<PxParseReply> responseStream,
        ServerCallContext context
    ) {
        _logger.LogInformation("Received gRPC request to subscribe {Symbols}", request.Symbols);

        var isSubscribed = await _clientAggregator.Subscribe(
            request.Symbols,
            message => responseStream.WriteAsync(new PxParseReply { Message = message }, context.CancellationToken)
        );

        if (!isSubscribed) {
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    $"Unable to subscribe to {request.Symbols} - no subscribe-able sources"
                )
            );
        }
    }
}