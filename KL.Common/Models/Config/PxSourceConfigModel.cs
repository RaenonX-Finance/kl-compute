using JetBrains.Annotations;
using KL.Common.Enums;

namespace KL.Common.Models.Config;


public record PxSourceConfigModel {
    [UsedImplicitly]
    public required PxSource Source { get; init; }

    [UsedImplicitly]
    public required ProductCategory ProductCategory { get; init; }

    [UsedImplicitly]
    public required bool Enabled { get; init; }

    /// <summary>
    ///     Symbol used internally for handling data.
    ///     This is used in various calculation processes, also stored in the database.
    /// </summary>
    /// <example>NQ</example>
    [UsedImplicitly]
    public required string InternalSymbol { get; init; }

    /// <summary>
    ///     Symbol used on 3rd party API for data control.
    /// </summary>
    /// <example>TC.F.CME.NQ.HOT</example>
    [UsedImplicitly]
    public required string ExternalSymbol { get; init; }

    [UsedImplicitly]
    public required string Name { get; init; }

    public static PxSourceConfigModel[] GenerateDefault() {
        return new[] {
            new PxSourceConfigModel {
                Source = PxSource.Touchance,
                Enabled = true,
                InternalSymbol = "NQ",
                ExternalSymbol = "TC.F.CME.NQ.HOT",
                Name = "小那",
                ProductCategory = ProductCategory.UsIndexFutures
            },
            new PxSourceConfigModel {
                Source = PxSource.Touchance,
                Enabled = false,
                InternalSymbol = "YM",
                ExternalSymbol = "TC.F.CBOT.YM.HOT",
                Name = "小道",
                ProductCategory = ProductCategory.UsIndexFutures
            },
            new PxSourceConfigModel {
                Source = PxSource.Touchance,
                Enabled = true,
                InternalSymbol = "FITX",
                ExternalSymbol = "TC.F.TWF.FITX.HOT",
                Name = "台指",
                ProductCategory = ProductCategory.TaiwanIndexFutures
            }
        };
    }
}