using KL.Common.Extensions;
using KL.GrpcCaller.Enums;
using KL.GrpcCaller.Extensions;
using KL.GrpcCaller.Workers;

namespace KL.GrpcCaller.Utils;


public static class Initializer {
    public static IHost Initialize(string[] args) {
        var app = Host
            .CreateDefaultBuilder(args)
            .BuildLogging()
            .BuildServices()
            .Build()
            .InitLogging();

        return app;
    }

    private static void BuildServices(IServiceCollection services, WorkerAction action) {
        switch (action) {
            case WorkerAction.Subscribe:
                services.AddHostedService<SubscribeWorker>();
                break;
            case WorkerAction.OptionsOi:
                services.AddHostedService<OptionsOiWorker>();
                break;
            case WorkerAction.FinancialEvents:
                services.AddHostedService<FinancialEventsWorker>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), $"Action `{action}` is unhandled");
        }
    }

    private static IHostBuilder BuildServices(this IHostBuilder builder) {
        builder.ConfigureServices((context, services) => BuildServices(services, context.Configuration.GetAction()));

        return builder;
    }
}