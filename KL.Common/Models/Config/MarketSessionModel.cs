using KL.Common.Enums;
using KL.Common.Extensions;

namespace KL.Common.Models.Config;


public record MarketSessionModel {
    public required TimeZoneInfo Timezone { get; init; }

    public required DayOfWeek[] TradingDays { get; init; }

    public required TimeOnly Start { get; init; }

    public required TimeOnly End { get; init; }
    
    public bool IsNowTradingSession() {
        var now = DateTime.UtcNow.ToTimezone(TimeZoneInfo.Utc).ToTimezone(Timezone);

        if (!TradingDays.Contains(now.DayOfWeek)) {
            return false;
        }

        var nowTime = TimeOnly.FromDateTime(now);

        if (Start > End) {
            // Cross day
            return nowTime > Start || nowTime < End;
        }
        
        return Start < nowTime && nowTime < End;
    }

    public static MarketSessionModel[] GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.UsIndexFutures => new[] {
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
            _ => Array.Empty<MarketSessionModel>()
        };
    }
}