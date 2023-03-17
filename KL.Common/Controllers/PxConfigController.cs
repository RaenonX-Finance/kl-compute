using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Models;
using KL.Common.Models.Config;
using MongoDB.Driver;
using Serilog;

namespace KL.Common.Controllers;


public static class PxConfigController {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PxConfigController));

    public static readonly PxConfigModel Config = GetConfig();

    public static readonly int[] EmaPeriods = GetEmaPeriods();

    private static PxConfigModel GetConfig() {
        var config = MongoConst.PxCalcConfig.AsQueryable().FirstOrDefault();

        if (config != default) {
            return config;
        }

        config = PxConfigModel.GenerateDefault();
        MongoConst.PxCalcConfig.InsertOne(config);

        return config;
    }

    private static int[] GetEmaPeriods() {
        return Config.GetType()
            .GetProperties()
            .Select(
                property =>
                    (property.GetValue(Config) as EmaPairConfigModel)?.EmaPeriods
                    ?? (property.GetValue(Config) as EmaPairConfigModel[])?.SelectMany(x => x.EmaPeriods)
                    .Distinct()
                    .ToArray()
            )
            .OfType<int[]>()
            .SelectMany(x => x)
            .Distinct()
            .ToArray();
    }

    public static string GetInternalSymbol(string externalSymbol, PxSource source) {
        try {
            return Config.SourceList
                .First(r => r.ExternalSymbol == externalSymbol && r.Source == source)
                .InternalSymbol;
        } catch (InvalidOperationException ex) {
            Log.Error(
                ex,
                "Unable to get internal symbol of {ExternalSymbol} sourced from {Source}",
                externalSymbol,
                source
            );
            throw;
        }
    }

    public static bool IsMarketOpened(string symbol) {
        return Config.MarketSessionMap[Config.Sources[symbol].ProductCategory].Any(r => r.IsNowTradingSession());
    }

    public static bool IsTimestampMarketDateCutoff(string symbol, DateTime timestamp) {
        var cutoff = Config.MarketDateCutoffMap[Config.Sources[symbol].ProductCategory];

        return TimeOnly
            .FromDateTime(timestamp.ToTimezoneFromUtc(cutoff.Timezone))
            // Add 1 minute because second of `timestamp` may not be exactly :00
            .IsBetween(cutoff.Time, cutoff.Time.AddMinutes(1));
    }
}