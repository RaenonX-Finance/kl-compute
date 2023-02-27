namespace KL.Common.Models;


public record PxRealtimeModel {
    public required decimal Open { get; init; }

    public required decimal High { get; init; }

    public required decimal Low { get; init; }

    public required decimal Close { get; init; }

    public decimal DiffVal => Close - Open;

    public decimal DiffPct => (Close / Open - 1) * 100;
}