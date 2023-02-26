using System.Reflection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace KL.Common.Utils;


internal class UtcTimestampEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propFactory) {
        logEvent.AddPropertyIfAbsent(propFactory.CreateProperty("TimestampUtc", logEvent.Timestamp.UtcDateTime));
    }
}

public static class LoggingHelper {
    private const string OutputTemplate =
        "{TimestampUtc:yyyy-MM-dd HH:mm:ss.fff} [{ThreadId,3}] "
        + "{SourceContext,50} [{Level:u1}] {Message:lj}{NewLine}{Exception}";

    public static void Initialize(string? logDir, IHostEnvironment? environment) {
        var appName = Assembly.GetCallingAssembly().GetName().FullName.Split(',')[0];
        var isDevelopment = environment?.IsDevelopment() ?? true;
        var isProduction = environment?.IsProduction() ?? false;
        
        if (isDevelopment) {
            appName += ".Development";
        } else if (isProduction) {
            appName += ".Production";
        }

        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithThreadId()
            .Enrich.With(new UtcTimestampEnricher())
            .MinimumLevel.Information();

        if (isDevelopment) {
            loggerConfig = loggerConfig.WriteTo.Console(outputTemplate: OutputTemplate);
        }

        if (EnvironmentConfigHelper.Config.Logging.NewRelicApiKey != null) {
            loggerConfig = loggerConfig.WriteTo.NewRelicLogs(
                applicationName: appName,
                licenseKey: EnvironmentConfigHelper.Config.Logging.NewRelicApiKey
            );
        }

        if (logDir != null) {
            loggerConfig = loggerConfig.WriteTo.File(
                Path.Combine(logDir, $"{appName}-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: OutputTemplate
            );
        }

        Log.Logger = loggerConfig.CreateLogger();
    }
}