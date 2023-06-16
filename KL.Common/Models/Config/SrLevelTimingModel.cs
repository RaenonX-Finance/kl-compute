using JetBrains.Annotations;
using KL.Common.Enums;

namespace KL.Common.Models.Config;


public record SrLevelTimingModel {
    [UsedImplicitly]
    public required TimeZoneInfo Timezone { get; init; }
    
    [UsedImplicitly]
    public required TimeOnly Open { get; init; }

    [UsedImplicitly]
    public required TimeOnly Close { get; init; }

    [UsedImplicitly]
    public IEnumerable<TimeOnly> Timings => new[] { Open, Close };

    public static SrLevelTimingModel GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.TaiwanIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                Open = new TimeOnly(8, 45),
                Close = new TimeOnly(13, 29)
            },
            ProductCategory.HongKongIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Hong_Kong"),
                Open = new TimeOnly(9, 20),
                Close = new TimeOnly(16, 29)
            },
            ProductCategory.UsFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                Open = new TimeOnly(8, 30),
                Close = new TimeOnly(14, 59)
            },
            ProductCategory.JapanIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo"),
                Open = new TimeOnly(9, 00),
                Close = new TimeOnly(14, 59)
            },
            ProductCategory.SingaporeTaiwanIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore"),
                Open = new TimeOnly(8, 45),
                Close = new TimeOnly(13, 29)
            },
            ProductCategory.EuroIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"),
                Open = new TimeOnly(9, 00),
                Close = new TimeOnly(17, 29)
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(productCategory),
                productCategory,
                "Given product category doesn't have default value for `SrLevelTimingModel`"
            )
        };
    }
}