namespace KL.Common.Structs;


public record TimeAtTimezone {
    public required TimeZoneInfo Timezone { get; init; }

    public required TimeOnly Time { get; init; }
}