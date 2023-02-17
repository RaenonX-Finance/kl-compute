using KL.Common.Structs;

namespace KL.Common.Models.Config;


public record MarketDateCutoffModel : TimeAtTimezone {
    public required int OffsetOnCutoff { get; init; }
}