using KL.GrpcCaller.Grpc;

namespace KL.GrpcCaller.Workers; 


public class FinancialEventsWorker : BaseWorker {
    public FinancialEventsWorker(
        ILogger<FinancialEventsWorker> logger,
        IConfiguration config,
        IHostApplicationLifetime application
    ) : base(logger, config, application) { }
    
    protected override async Task Main(CancellationToken cancellationToken) {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);

        Logger.LogInformation("To get financial events @ {Date}", date);

        var gracefullyExited = await GrpcPxInfoCaller.GetFinancialEvents(date, cancellationToken);

        if (gracefullyExited) {
            Logger.LogInformation("Fetched financial events @ {Date}", date);
        } else {
            Logger.LogError("Request to get financial events @ {Date} did not complete", date);
        }
    }
}