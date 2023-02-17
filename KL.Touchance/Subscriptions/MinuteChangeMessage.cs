using JetBrains.Annotations;
using KL.Touchance.Interfaces;

namespace KL.Touchance.Subscriptions;


public record MinuteChangeMessage : TcSubscription, ITimestamp {
    [UsedImplicitly]
    public required int Date { get; init; }

    [UsedImplicitly]
    public required int Time { get; init; }
}