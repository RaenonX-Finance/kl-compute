using KL.Common.Enums;
using KL.Common.Structs;

namespace KL.Common.Models.Config;


public record MarketSessionModel {
    public required DayOfWeek[] TradingDays { get; init; }

    public required TimeAtTimezone Start { get; init; }

    public required TimeAtTimezone End { get; init; }

    public static MarketSessionModel[] GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.UsIndexFutures => new[] {
                new MarketSessionModel {
                    TradingDays = new[] {
                        DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday
                    },
                    Start = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                        Time = new TimeOnly(17, 00)
                    },
                    End = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                        Time = new TimeOnly(16, 00)
                    }
                }
            },
            ProductCategory.TaiwanIndexFutures => new[] {
                new MarketSessionModel {
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(8, 45)
                    },
                    End = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(13, 45)
                    }
                },
                new MarketSessionModel {
                    TradingDays = new[] {
                        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                    },
                    Start = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(15, 00)
                    },
                    End = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(5, 45)
                    }
                }
            },
            _ => Array.Empty<MarketSessionModel>()
        };
    }
}