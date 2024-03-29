﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.File.Archive;

namespace KL.Common.Utils;


internal class UtcTimestampEnricher : ILogEventEnricher {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propFactory) {
        logEvent.AddPropertyIfAbsent(propFactory.CreateProperty("TimestampUtc", logEvent.Timestamp.UtcDateTime));
    }
}

public static class LoggingHelper {
    private const string OutputTemplate =
        "{TimestampUtc:yyyy-MM-dd HH:mm:ss.fff} [{ProcessId,6}] [{ThreadId,3}] "
        + "{SourceContext,50} [{Level:u1}] {Message:lj}{NewLine}{Exception}";

    public static void Initialize(string? logDir, bool isDev, bool isProd, IConfiguration? config) {
        var appName = AppNameManager.GetAppName(isDev, isProd);

        var loggerConfig = new LoggerConfiguration()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.With(new UtcTimestampEnricher())
            .MinimumLevel.Information();

        if (isDev) {
            loggerConfig = loggerConfig.WriteTo.Console(outputTemplate: OutputTemplate);
        }

        if (EnvironmentConfigHelper.Config.Logging.NewRelicApiKey is not null) {
            loggerConfig = loggerConfig.WriteTo.NewRelicLogs(
                applicationName: appName,
                licenseKey: EnvironmentConfigHelper.Config.Logging.NewRelicApiKey
            );
        }

        if (logDir is not null) {
            loggerConfig = loggerConfig.WriteTo.File(
                Path.Combine(logDir, $"{appName}-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: OutputTemplate,
                // Only files over the limit of Serilog retain will trigger hook, causing the log files to be archived
                // VM already have script to clean up the log files
                retainedFileCountLimit: 1,
                // Default is 1 GB only
                fileSizeLimitBytes: null, 
                hooks: new ArchiveHooks()
            );
        }

        if (config is not null) {
            loggerConfig = loggerConfig.ReadFrom.Configuration(config);
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    public static void Initialize(string? logDir, IHostEnvironment env, IConfiguration config) {
        Initialize(logDir, env.IsDevelopment(), env.IsProduction(), config);
    }
}