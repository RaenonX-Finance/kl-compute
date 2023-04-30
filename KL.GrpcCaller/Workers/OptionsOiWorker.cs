using KL.GrpcCaller.Extensions;
using KL.GrpcCaller.Grpc;

namespace KL.GrpcCaller.Workers;


public class OptionsOiWorker : BaseWorker {
    public OptionsOiWorker(
        ILogger<OptionsOiWorker> logger,
        IConfiguration config,
        IHostApplicationLifetime application
    ) : base(logger, config, application) { }

    protected override async Task Main(CancellationToken cancellationToken) {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var symbol in Config.GetSymbols()) {
            Logger.LogInformation("To get options OI of {Symbol} @ {Date}", symbol, date);

            var gracefullyExited = await GrpcPxInfoCaller.GetOptionsOi(date, symbol, cancellationToken);

            if (gracefullyExited) {
                Logger.LogInformation("Fetched options OI of {Symbol} @ {Date}", symbol, date);
            } else {
                Logger.LogError("Request to get options OI of {Symbol} @ {Date} did not complete", symbol, date);
            }
        }
    }
}