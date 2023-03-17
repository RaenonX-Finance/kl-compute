using KL.Common.Enums;
using KL.Common.Interfaces;
using KL.Touchance.Extensions;

namespace KL.Touchance.Models;


public record PxHistoryRequestIdentifier : IHistoryMetadata {
    private readonly DateTime _start;
    private readonly DateTime _end;

    public required DateTime Start {
        get => _start;
        // History data handler needs this to check request identity 
        init => _start = value.ToTouchanceDatetime();
    }

    public required DateTime End {
        get => _end;
        // History data handler needs this to check request identity
        init => _end = value.ToTouchanceDatetime();
    }

    public required string Symbol { get; init; }

    public required HistoryInterval Interval { get; init; }
}