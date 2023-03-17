using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Interfaces;
using KL.Touchance.Extensions;
using KL.Touchance.Models;

namespace KL.Touchance.Subscriptions;


public abstract record PxHistoryReadyMessage : TcSubscription, IHistoryMetadata {
    private PxHistoryRequestIdentifier? _identifier;
    private string? _identifierString;

    [UsedImplicitly]
    public required string StartTime { get; init; }

    [UsedImplicitly]
    public required string EndTime { get; init; }

    [UsedImplicitly]
    public required string Status { get; init; }

    public bool IsReady => Status == "Ready";

    public PxHistoryRequestIdentifier Identifier =>
        _identifier ??= new PxHistoryRequestIdentifier {
            Start = Start,
            End = End,
            Symbol = Symbol,
            Interval = Interval
        };

    public string IdentifierString {
        get {
            IHistoryMetadata metadata = this;

            return _identifierString ??= metadata.ToIdentifier();
        }
    }

    [UsedImplicitly]
    public required string Symbol { get; init; }

    public abstract HistoryInterval Interval { get; }

    public DateTime Start => StartTime.FromTouchanceHourlyPrecision();

    public DateTime End => EndTime.FromTouchanceHourlyPrecision();
}