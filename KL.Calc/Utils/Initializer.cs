using KL.Calc.Services;
using KL.Common.Extensions;
using KL.Common.Utils;

namespace KL.Calc.Utils; 


public static class Initializer {
    public static async Task<WebApplication> Initialize(string[] args) {
        var app = WebApplication
            .CreateBuilder(args)
            .BuildLogging()
            .BuildGrpcService(EnvironmentConfigHelper.Config.Grpc.CalcPort)
            .BuildApp()
            .InitLogging()
            .InitEnforceSingleton()
            .InitGrpcService<PxDataService>()
            .InitEndpoints();

        await CommonInitializer.Initialize();

        return app;
    }
}