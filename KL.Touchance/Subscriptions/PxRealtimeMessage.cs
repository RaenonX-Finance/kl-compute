using JetBrains.Annotations;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Models;
using KL.Touchance.Extensions;

namespace KL.Touchance.Subscriptions;


public record PxRealtimeData {
    public DateTime FilledTime => $"{TradeDate} {PreciseTime:D12}".FromTouchanceRealtime();

    // US futures use `ReferencePrice` as open instead
    public decimal Open => string.IsNullOrEmpty(OpeningPrice) ? ReferencePrice : Convert.ToDecimal(OpeningPrice);

    public decimal High => string.IsNullOrEmpty(HighPrice) ? 0 : Convert.ToDecimal(HighPrice);

    public decimal Low => string.IsNullOrEmpty(LowPrice) ? 0 : Convert.ToDecimal(LowPrice);

    public decimal Close => string.IsNullOrEmpty(ClosingPrice) ? 
        string.IsNullOrEmpty(TradingPrice) ? 0 : Convert.ToDecimal(TradingPrice) : 
        Convert.ToDecimal(ClosingPrice);

    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    // Taking string and convert later because the JSON value might be empty
    public required string TradingPrice { get; init; }

    [UsedImplicitly]
    // Taking string and convert later because the JSON value might be empty
    public required string OpeningPrice { get; init; }

    [UsedImplicitly]
    // Taking string and convert later because the JSON value might be empty
    public required string HighPrice { get; init; }

    [UsedImplicitly]
    // Taking string and convert later because the JSON value might be empty
    public required string LowPrice { get; init; }

    [UsedImplicitly]
    // Taking string and convert later because the JSON value might be empty
    public required string ClosingPrice { get; init; }

    [UsedImplicitly]
    public required decimal ReferencePrice { get; init; }

    [UsedImplicitly]
    public required int TradeQuantity { get; init; }

    [UsedImplicitly]
    public required long TradeDate { get; init; }

    [UsedImplicitly]
    public required long PreciseTime { get; init; }
}

public record PxRealtimeMessage : TcSubscription {
    [UsedImplicitly]
    public required PxRealtimeData Quote { get; init; }

    private bool IsTraded => Quote.TradeQuantity > 0;
    
    public RealtimeEventArgs? ToRealtimeEventArgs() {
        if (!IsTraded) {
            return null;
        }
        
        return new RealtimeEventArgs {
            Symbol = PxConfigController.GetInternalSymbol(Quote.Symbol, PxSource.Touchance),
            Data = new PxRealtimeModel {
                Open = Quote.Open,
                High = Quote.High,
                Low = Quote.Low,
                Close = Quote.Close
            },
            IsTriggeredByHistory = false
        };
    }
}