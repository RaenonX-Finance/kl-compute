using JetBrains.Annotations;
using KL.Touchance.Extensions;

namespace KL.Touchance.Subscriptions;


public record PxRealtimeData {
    public decimal Last => TradingPrice;

    public DateTime FilledTime => $"{TradeDate} {PreciseTime:D12}".FromTouchanceRealtime();
    
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required decimal TradingPrice { get; init; }

    [UsedImplicitly]
    public required decimal TradeDate { get; init; }

    [UsedImplicitly]
    public required decimal PreciseTime { get; init; }
}


public record PxRealtimeMessage : TcSubscription {
    [UsedImplicitly]
    public required PxRealtimeData Quote { get; init; }
}