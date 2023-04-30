namespace KL.GrpcCaller.Workers; 


public abstract class BaseWorker : IHostedService {
    protected readonly ILogger<BaseWorker> Logger;

    protected readonly IConfiguration Config;

    protected readonly IHostApplicationLifetime Application;

    protected BaseWorker(
        ILogger<BaseWorker> logger,
        IConfiguration config,
        IHostApplicationLifetime application
    ) {
        logger.LogInformation("Initiating {Worker}", GetType().Name);

        Logger = logger;
        Config = config;
        Application = application;
    }

    protected abstract Task Main(CancellationToken cancellationToken);

    public async Task StartAsync(CancellationToken cancellationToken) {
        await Main(cancellationToken);
        
        Application.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}