using System.Net;
using KL.Common.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace KL.Common.Extensions;


public static class InitializingExtensions {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(InitializingExtensions));
    
    public static WebApplicationBuilder BuildGrpcService(this WebApplicationBuilder builder, int grpcPort) {
        builder.Services.AddGrpc();
        if (builder.Environment.IsDevelopment()) {
            builder.Services.AddGrpcReflection();
        }

        builder.WebHost.ConfigureKestrel(options => { options.Listen(IPAddress.Loopback, grpcPort); });

        return builder;
    }

    public static IHostBuilder BuildLogging(this IHostBuilder builder) {
        builder.UseSerilog();

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

    public static IHost InitLogging(this IHost app) {
        LoggingHelper.Initialize(
            EnvironmentConfigHelper.Config.Logging.OutputDirectory,
            app.Services.GetRequiredService<IHostEnvironment>(),
            app.Services.GetRequiredService<IConfiguration>()
        );

        return app;
    }

    public static WebApplication InitLogging(this WebApplication app) {
        LoggingHelper.Initialize(
            EnvironmentConfigHelper.Config.Logging.OutputDirectory,
            app.Environment,
            app.Configuration
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
        
        Log.Information("Booting {AppName}", AppNameManager.GetAppName(app));
        
        app.Lifetime.ApplicationStopping.Register(Serilog.Log.CloseAndFlush);

        return app;
    }

    public static WebApplication InitGrpcService<T>(this WebApplication app) where T : class {
        app.MapGrpcService<T>();
        if (app.Environment.IsDevelopment()) {
            app.MapGrpcReflectionService();
        }

        return app;
    }

    public static WebApplication InitEnforceSingleton(this WebApplication app) {
        AppSingletonEnforcer.Enforce(AppNameManager.GetAppName(app));

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