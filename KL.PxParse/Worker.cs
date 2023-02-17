using KL.PxParse.Controllers;

namespace KL.PxParse;


public class Worker : BackgroundService {
    protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
        await new ClientAggregator(cancellationToken).Start();
    }
}