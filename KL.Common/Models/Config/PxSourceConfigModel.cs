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

    [UsedImplicitly]
    public required bool EnableRealtime { get; init; }

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

    public static IDictionary<string, PxSourceConfigModel> GenerateDefault() {
        return new Dictionary<string, PxSourceConfigModel> {
            {
                "FITX",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = true,
                    EnableRealtime = true,
                    InternalSymbol = "FITX",
                    ExternalSymbol = "TC.F.TWF.FITX.HOT",
                    Name = "台指",
                    ProductCategory = ProductCategory.TaiwanIndexFutures
                }
            }, {
                "NQ",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = true,
                    EnableRealtime = true,
                    InternalSymbol = "NQ",
                    ExternalSymbol = "TC.F.CME.NQ.HOT",
                    Name = "小那",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "YM",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = true,
                    EnableRealtime = true,
                    InternalSymbol = "YM",
                    ExternalSymbol = "TC.F.CBOT.YM.HOT",
                    Name = "小道",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "ES",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "ES",
                    ExternalSymbol = "TC.F.CME.ES.HOT",
                    Name = "SP",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "RTY",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "RTY",
                    ExternalSymbol = "TC.F.CME.RTY.HOT",
                    Name = "羅素",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "DX",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "DX",
                    ExternalSymbol = "TC.F.NYBOT.DX.HOT",
                    Name = "美元指數",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "GC",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "GC",
                    ExternalSymbol = "TC.F.CME.GC.HOT",
                    Name = "黃金",
                    ProductCategory = ProductCategory.UsFutures
                }
            }, {
                "FTSE-TW",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "GC",
                    ExternalSymbol = "TC.F.SGXQ.TWN.HOT",
                    Name = "富時台",
                    ProductCategory = ProductCategory.SingaporeTaiwanIndexFutures
                }
            }, {
                "NK",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "NK",
                    ExternalSymbol = "TC.F.OSE.NK225.HOT",
                    Name = "日經",
                    ProductCategory = ProductCategory.JapanIndexFutures
                }
            }, {
                "NKM",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "NKM",
                    ExternalSymbol = "TC.F.OSE.NK225M.HOT",
                    Name = "小日經",
                    ProductCategory = ProductCategory.JapanIndexFutures
                }
            }, {
                "FDAX",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "FDAX",
                    ExternalSymbol = "TC.F.EUREX.FDAX.HOT",
                    Name = "德指",
                    ProductCategory = ProductCategory.EuroIndexFutures
                }
            }, {
                "FESX",
                new PxSourceConfigModel {
                    Source = PxSource.Touchance,
                    Enabled = false,
                    EnableRealtime = true,
                    InternalSymbol = "FESX",
                    ExternalSymbol = "TC.F.EUREX.FESX.HOT",
                    Name = "STOXX 50",
                    ProductCategory = ProductCategory.EuroIndexFutures
                }
            }
        };
    }
}