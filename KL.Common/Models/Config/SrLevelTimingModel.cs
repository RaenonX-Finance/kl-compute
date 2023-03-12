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
                // Needs to be 29 instead of 30 because it's close of 13:29 being used
                Close = new TimeOnly(13, 29)
            },
            ProductCategory.UsIndexFutures => new SrLevelTimingModel {
                Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                Open = new TimeOnly(8, 30),
                // Needs to be 59 instead of 00 because it's close of 14:59 being used
                Close = new TimeOnly(14, 59)
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(productCategory),
                productCategory,
                "Product category doesn't have default value"
            )
        };
    }
}