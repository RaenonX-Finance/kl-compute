using System.Net;
using KL.Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace KL.Common.Extensions;


public static class InitializingExtensions {
    public static WebApplicationBuilder BuildGrpcService(this WebApplicationBuilder builder, int grpcPort) {
        builder.Services.AddGrpc();
        builder.WebHost.ConfigureKestrel(options => { options.Listen(IPAddress.Loopback, grpcPort); });

        return builder;
    }

    public static WebApplicationBuilder BuildLogging(this WebApplicationBuilder builder) {
        builder.Host.UseSerilog();

        return builder;
    }

    public static WebApplication BuildApp(this WebApplicationBuilder builder) {
        var app = builder.Build();

        return app;
    }

    public static WebApplication InitLogging(this WebApplication app) {
        LoggingHelper.Initialize(EnvironmentConfigHelper.Config.Logging.OutputDirectory, app.Environment);

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

    public static WebApplication InitGrpcService<T>(this WebApplication app) where T : class {
        app.MapGrpcService<T>();

        return app;
    }

    public static WebApplication InitEndpoints(this WebApplication app) {
        app.MapGet(
            "/",
            () => "gRPC client required."
        );

        return app;
    }
}