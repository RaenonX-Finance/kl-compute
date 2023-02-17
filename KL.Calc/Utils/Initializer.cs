using System.Net;
using KL.Calc.Services;
using KL.Common.Controllers;
using KL.Common.Utils;
using Serilog;

namespace KL.Calc.Utils;


public static class Initializer {
    public static async Task<WebApplication> Initialize(string[] args) {
        await MongoManager.Initialize();

        return WebApplication
            .CreateBuilder(args)
            .BuildLogging()
            .BuildGrpcService()
            .BuildApp()
            .InitLogging()
            .InitGrpcService()
            .InitEndpoints();
    }

    private static WebApplicationBuilder BuildGrpcService(this WebApplicationBuilder builder) {
        builder.Services.AddGrpc();
        builder.WebHost.ConfigureKestrel(
            options => { options.Listen(IPAddress.Loopback, EnvironmentConfigHelper.Config.Grpc.CalcPort); }
        );

        return builder;
    }

    private static WebApplicationBuilder BuildLogging(this WebApplicationBuilder builder) {
        builder.Host.UseSerilog();

        return builder;
    }

    private static WebApplication BuildApp(this WebApplicationBuilder builder) {
        var app = builder.Build();

        return app;
    }

    private static WebApplication InitLogging(this WebApplication app) {
        LoggingHelper.Initialize(
            EnvironmentConfigHelper.Config.Logging.OutputDirectory,
            app.Environment.IsDevelopment()
        );

        app.UseSerilogRequestLogging(
            options => {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) => {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                };

                // Default props: RequestMethod, RequestPath, StatusCode, Elapsed
                options.MessageTemplate = "HTTP {RequestMethod} `{RequestPath}` responded {StatusCode} "
                                          + "in {Elapsed:0.00} ms from {RequestHost}";
            }
        );

        return app;
    }

    private static WebApplication InitGrpcService(this WebApplication app) {
        app.MapGrpcService<PxDataService>();

        return app;
    }

    private static WebApplication InitEndpoints(this WebApplication app) {
        app.MapGet(
            "/",
            () => "gRPC client required."
        );

        return app;
    }
}