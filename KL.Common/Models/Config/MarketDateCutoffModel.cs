using KL.Common.Enums;

namespace KL.Common.Models.Config;


public record MarketDateCutoffModel {
    public required TimeZoneInfo Timezone { get; init; }

    public required TimeOnly Time { get; init; }

    public required int OffsetOnCutoff { get; init; }

    public static MarketDateCutoffModel GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.UsFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                Time = new TimeOnly(17, 00),
                OffsetOnCutoff = 1
            },
            ProductCategory.TaiwanIndexFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                Time = new TimeOnly(8, 45),
                OffsetOnCutoff = 0
            },
            ProductCategory.HongKongIndexFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong"),
                Time = new TimeOnly(17, 15),
                OffsetOnCutoff = 1
            },
            ProductCategory.JapanIndexFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
                Time = new TimeOnly(8, 45),
                OffsetOnCutoff = 1
            },
            ProductCategory.SingaporeTaiwanIndexFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore"),
                Time = new TimeOnly(9, 00),
                OffsetOnCutoff = 1
            },
            ProductCategory.EuroIndexFutures => new MarketDateCutoffModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"),
                Time = new TimeOnly(1, 00),
                OffsetOnCutoff = 1
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(productCategory),
                productCategory,
                "Given product category doesn't have default value for `MarketDateCutoffModel`"
            )
        };
    }
}