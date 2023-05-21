using KL.Common.Extensions;
using KL.Common.Utils;
using KL.PxParse.Controllers;
using KL.PxParse.Interfaces;
using KL.PxParse.Services;

namespace KL.PxParse.Utils;


public static class Initializer {
    public static async Task<WebApplication> Initialize(string[] args) {
        var app = await WebApplication
            .CreateBuilder(args)
            .BuildLogging()
            .BuildServices()
            .BuildGrpcService(EnvironmentConfigHelper.Config.Grpc.PxParsePort)
            .BuildApp()
            .InitLogging()
            .InitEnforceSingleton()
            .InitGrpcService<PxParseGrpcService>()
            .InitEndpoints()
            .InitClientAggregator();

        return app;
    }

    private static async Task<WebApplication> InitClientAggregator(this WebApplication app) {
        await CommonInitializer.Initialize();
        await app.Services.GetRequiredService<IClientAggregator>().Start();
        
        return app;
    }
    
    private static WebApplicationBuilder BuildServices(this WebApplicationBuilder builder) {
        builder.Services.AddSingleton<IClientAggregator, ClientAggregator>();
        
        return builder;
    }
}