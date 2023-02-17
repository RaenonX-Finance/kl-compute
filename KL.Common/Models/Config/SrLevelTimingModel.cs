using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Structs;

namespace KL.Common.Models.Config;


public record SrLevelTimingPair {
    public required TimeAtTimezone Open { get; init; }

    public required TimeAtTimezone Close { get; init; }

    public TimeAtTimezone[] Timings => new[] { Open, Close };
}

public record SrLevelTimingModel {
    [UsedImplicitly]
    public required SrLevelTimingPair Primary { get; init; }

    public SrLevelTimingPair? Secondary { get; init; }

    public static SrLevelTimingModel GenerateDefault(ProductCategory productCategory) {
        return productCategory switch {
            ProductCategory.TaiwanIndexFutures => new SrLevelTimingModel {
                Primary = new SrLevelTimingPair {
                    Open = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(8, 45)
                    },
                    Close = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(13, 30)
                    }
                },
                Secondary = new SrLevelTimingPair {
                    Open = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(15, 00)
                    },
                    Close = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei"),
                        Time = new TimeOnly(13, 30)
                    }
                }
            },
            ProductCategory.UsIndexFutures => new SrLevelTimingModel {
                Primary = new SrLevelTimingPair {
                    Open = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                        Time = new TimeOnly(8, 30)
                    },
                    Close = new TimeAtTimezone {
                        Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago"),
                        Time = new TimeOnly(15, 00)
                    }
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