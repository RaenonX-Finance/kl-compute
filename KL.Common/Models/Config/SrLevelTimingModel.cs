using JetBrains.Annotations;
using KL.Common.Enums;

namespace KL.Common.Models.Config;


public record SrLevelTimingPair {
    public required TimeZoneInfo Timezone { get; init; }
    
    public required TimeOnly Open { get; init; }

    public required TimeOnly Close { get; init; }

    public IEnumerable<TimeOnly> Timings => new[] { Open, Close };
}

public record SrLevelTimingModel {
    [UsedImplicitly]
    public required SrLevelTimingPair Primary { get; init; }

    [UsedImplicitly]
    public SrLevelTimingPair? Secondary { get; init; }

    public SrLevelTimingPair? GetTimingPair(SrLevelType srLevelType) {
        return srLevelType switch {
            SrLevelType.Primary => Primary,
            SrLevelType.Secondary => Secondary,
            _ => throw new ArgumentOutOfRangeException(
                nameof(srLevelType),
                srLevelType,
                "Given SR level type does not have corresponding timing config to use"
            )
        };
    }

    public static SrLevelTimingModel GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.TaiwanIndexFutures => new SrLevelTimingModel {
                Primary = new SrLevelTimingPair {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                    Open = new TimeOnly(8, 45),
                    // Needs to be 29 instead of 30 because it's close of 13:29 being used
                    Close = new TimeOnly(13, 29)
                },
                Secondary = new SrLevelTimingPair {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                    Open =new TimeOnly(15, 00),
                    // Needs to be 29 instead of 30 because it's close of 13:29 being used
                    Close = new TimeOnly(13, 29)
                }
            },
            ProductCategory.UsIndexFutures => new SrLevelTimingModel {
                Primary = new SrLevelTimingPair {
                    Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                    Open = new TimeOnly(8, 30),
                    // Needs to be 59 instead of 00 because it's close of 14:59 being used
                    Close = new TimeOnly(14, 59)
                }
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(productCategory),
                productCategory,
                "Product category doesn't have default value"
            )
        };
    }
}