using JetBrains.Annotations;

namespace KL.Touchance.Subscriptions;


public record SymbolClearData {
    [UsedImplicitly]
    public required string Symbol { get; init; }
}

public record SymbolClearMessage : TcSubscription {
    [UsedImplicitly]
    public required SymbolClearData Data { get; init; }

    public string SymbolToSubscribe => $"{Data.Symbol}.HOT";
}