namespace KL.Common.Models.Config;


public record MarketDateCutoffModel {
    public required TimeZoneInfo Timezone { get; init; }

    public required TimeOnly Time { get; init; }

    public required int OffsetOnCutoff { get; init; }
}