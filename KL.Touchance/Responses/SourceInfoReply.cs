using System.Text.Json.Serialization;
using JetBrains.Annotations;
using KL.Touchance.Extensions;

namespace KL.Touchance.Responses;


public record ProductInfo {
    private int? _decimals;

    [UsedImplicitly]
    public decimal TickSize { get; init; }

    [UsedImplicitly]
    public required int TicksPerPoint { get; init; }

    [UsedImplicitly]
    [JsonPropertyName("EXG")]
    public required string ExchangeSymbol { get; init; }

    [UsedImplicitly]
    [JsonPropertyName("EXGName.CHT")]
    public required string ExchangeName { get; init; }

    public int Decimals {
        get {
            if (_decimals is not null) {
                return (int)_decimals;
            }

            var ticksPerPoint = TicksPerPoint;

            var decimals = 0;
            while ((ticksPerPoint /= 10) >= 1) {
                decimals++;
            }

            return _decimals ??= decimals;
        }
    }
}

public record SourceInfoReply : TcActionReply {
    private ProductInfo? _productInfo;

    [UsedImplicitly]
    public required Dictionary<string, Dictionary<string, string>> Info { get; init; }

    public ProductInfo ProductInfo {
        get { return _productInfo ??= Info.First(r => r.Key.Count(c => c == '.') == 3).Value.ToModel<ProductInfo>(); }
    }
}