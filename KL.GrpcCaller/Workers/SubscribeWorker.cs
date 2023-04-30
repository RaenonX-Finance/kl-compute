using KL.GrpcCaller.Extensions;
using KL.GrpcCaller.Grpc;

namespace KL.GrpcCaller.Workers;


public class SubscribeWorker : BaseWorker {
    public SubscribeWorker(
        ILogger<SubscribeWorker> logger,
        IConfiguration config,
        IHostApplicationLifetime application
    ) : base(logger, config, application) { }
    
    protected override async Task Main(CancellationToken cancellationToken) {
        var symbols = Config.GetSymbols();

        Logger.LogInformation("To subscribe {@Symbols}", symbols);

        var gracefullyExited = await GrpcPxParseCaller.Subscribe(symbols, cancellationToken);

        if (gracefullyExited) {
            Logger.LogInformation("Subscribed to {@Symbols}", symbols);
        } else {
            Logger.LogError("Request to subscribe {@Symbols} did not complete", symbols);
        }
    }
}