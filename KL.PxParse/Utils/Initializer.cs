using KL.Common.Utils;
using Serilog;

namespace KL.PxParse.Utils;


public static class Initializer {
    public static async Task<IHost> Initialize(string[] args) {
        var app = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddHostedService<Worker>())
            .UseSerilog()
            .Build()
            .InitLog();
        
        await CommonInitializer.Initialize();

        return app;
    }

    private static IHost InitLog(this IHost app) {
        LoggingHelper.Initialize(
            EnvironmentConfigHelper.Config.Logging.OutputDirectory,
            app.Services.GetRequiredService<IHostEnvironment>()
        );

        return app;
    }
}