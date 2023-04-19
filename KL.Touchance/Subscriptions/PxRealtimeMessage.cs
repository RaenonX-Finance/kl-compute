using JetBrains.Annotations;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Events;
using KL.Common.Models;
using KL.Touchance.Extensions;

namespace KL.Touchance.Subscriptions;


public record PxRealtimeData {
    public DateTime FilledTimestamp => $"{TradeDate} {FilledTime:D6}".FromTouchanceRealtime();

    // US futures use `ReferencePrice` as open instead
    public decimal Open => string.IsNullOrEmpty(OpeningPrice) ? 
        string.IsNullOrEmpty(ReferencePrice) ? 0 : Convert.ToDecimal(ReferencePrice) : 
        Convert.ToDecimal(OpeningPrice);

    public decimal High => string.IsNullOrEmpty(HighPrice) ? 0 : Convert.ToDecimal(HighPrice);

    public decimal Low => string.IsNullOrEmpty(LowPrice) ? 0 : Convert.ToDecimal(LowPrice);

    // Needs to take `TradingPrice` first because `ClosingPrice` of FITX will not update during US market hours
    public decimal Close => string.IsNullOrEmpty(TradingPrice) ? 
        string.IsNullOrEmpty(ClosingPrice) ? 0 : Convert.ToDecimal(ClosingPrice) : 
        Convert.ToDecimal(TradingPrice);

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
    // Taking string and convert later because the JSON value might be empty
    public required string ReferencePrice { get; init; }

    [UsedImplicitly]
    public required int TradeQuantity { get; init; }

    [UsedImplicitly]
    public required long TradeDate { get; init; }

    [UsedImplicitly]
    public required long FilledTime { get; init; }
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
            Timestamp = Quote.FilledTimestamp,
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