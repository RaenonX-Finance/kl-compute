using JetBrains.Annotations;
using KL.Common.Enums;

namespace KL.Common.Models.Config;


public record PxSourceConfigModel {
    public required PxSource Source { get; init; }

    public required ProductCategory ProductCategory { get; init; }

    public required bool Enabled { get; init; }

    public required bool EnableRealtime { get; init; }

    /// <summary>
    ///     Symbol used internally for handling data.
    ///     This is used in various calculation processes, also stored in the database.
    /// </summary>
    /// <example>NQ</example>
    public required string InternalSymbol { get; init; }

    /// <summary>
    ///     Symbol used on 3rd party API for data control.
    /// </summary>
    /// <example>TC.F.CME.NQ.HOT</example>
    public required string ExternalSymbol { get; init; }

    [UsedImplicitly]
    public required string Name { get; init; }

    public required decimal SrLevelMinDiff { get; init; }

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
                    ProductCategory = ProductCategory.TaiwanIndexFutures,
                    SrLevelMinDiff = 35
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = 35
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = 35
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = 4
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = 2
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = (decimal)0.1
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
                    ProductCategory = ProductCategory.UsFutures,
                    SrLevelMinDiff = 2
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
                    ProductCategory = ProductCategory.SingaporeTaiwanIndexFutures,
                    SrLevelMinDiff = 1
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
                    ProductCategory = ProductCategory.JapanIndexFutures,
                    SrLevelMinDiff = 15
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
                    ProductCategory = ProductCategory.JapanIndexFutures,
                    SrLevelMinDiff = 15
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
                    ProductCategory = ProductCategory.EuroIndexFutures,
                    SrLevelMinDiff = 30
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
                    ProductCategory = ProductCategory.EuroIndexFutures,
                    SrLevelMinDiff = 3
                }
            }
        };
    }
}