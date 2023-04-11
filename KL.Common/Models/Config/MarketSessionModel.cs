using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Extensions;

namespace KL.Common.Models.Config;


public record MarketSessionModel {
    [UsedImplicitly]
    public required TimeZoneInfo Timezone { get; init; }

    [UsedImplicitly]
    public required DayOfWeek[] TradingDays { get; init; }

    [UsedImplicitly]
    public required TimeOnly Start { get; init; }

    [UsedImplicitly]
    public required TimeOnly End { get; init; }
    
    public bool IsNowTradingSession() {
        var now = DateTime.UtcNow.ToTimezone(TimeZoneInfo.Utc).ToTimezone(Timezone);
        var nowTime = TimeOnly.FromDateTime(now);

        // Same day
        if (Start <= End) {
            return TradingDays.Contains(now.DayOfWeek) && Start < nowTime && nowTime < End;
        }

        // Cross day
        if (TradingDays.Contains(now.DayOfWeek) && nowTime > Start) {
            return true;
        }

        return TradingDays.Contains(now.DayOfWeek.GetPrev()) && nowTime < End;
    }

    public static MarketSessionModel[] GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.UsFutures => new[] {
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                    TradingDays = new[] {
                        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday
                    },
                    Start = new TimeOnly(17, 00),
                    End = new TimeOnly(16, 00)
                }
            },
            ProductCategory.TaiwanIndexFutures => new[] {
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(8, 45),
                    End = new TimeOnly(13, 45)
                },
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(15, 00),
                    End = new TimeOnly(5, 45)
                }
            },
            ProductCategory.JapanIndexFutures => new[] {
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(8, 45),
                    End = new TimeOnly(15, 15)
                },
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(16, 30),
                    End = new TimeOnly(6, 00)
                }
            },
            ProductCategory.SingaporeTaiwanIndexFutures => new[] {
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(9, 00),
                    End = new TimeOnly(12, 00)
                },
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(13, 00),
                    End = new TimeOnly(17, 00)
                }
            },
            ProductCategory.EuroIndexFutures => new[] {
                new MarketSessionModel {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"),
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeOnly(1, 00),
                    End = new TimeOnly(22, 30)
                }
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(productCategory),
                productCategory,
                "Given product category doesn't have default value for `MarketSessionModel`"
            )
        };
    }
}