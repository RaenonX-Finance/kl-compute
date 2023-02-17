using KL.Common.Enums;
using KL.Common.Interfaces;

namespace KL.Touchance.Models;


public record PxHistoryRequestIdentifier : IHistoryMetadata {
    public required DateTime Start { get; init; }

    public required DateTime End { get; init; }

    public required string Symbol { get; init; }

    public required HistoryInterval Interval { get; init; }
}