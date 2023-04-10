using KL.GrpcCaller.Extensions;
using KL.GrpcCaller.Grpc;

namespace KL.GrpcCaller.Workers;


public class SubscribeWorker : IHostedService {
    private readonly ILogger<SubscribeWorker> _logger;

    private readonly IConfiguration _config;

    private readonly IHostApplicationLifetime _application;

    public SubscribeWorker(
        ILogger<SubscribeWorker> logger,
        IConfiguration config,
        IHostApplicationLifetime application
    ) {
        logger.LogInformation("Initiating {Worker}", nameof(SubscribeWorker));

        _logger = logger;
        _config = config;
        _application = application;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        var symbols = _config.GetSymbols();

        _logger.LogInformation("To subscribe {@Symbols}", symbols);

        var gracefullyExited = await GrpcPxParseCaller.Subscribe(symbols, cancellationToken);

        if (gracefullyExited) {
            _logger.LogInformation("Subscribed to {@Symbols}", symbols);
        } else {
            _logger.LogError("Request to subscribe {@Symbols} did not complete", symbols);
        }
        _application.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}