using JetBrains.Annotations;

namespace KL.Touchance.Responses;


public record SourceInfoReply : TcActionReply {
    private Dictionary<string, string>? _productInfo;
    private decimal? _tick;
    private int? _decimals;

    [UsedImplicitly]
    public required Dictionary<string, Dictionary<string, string>> Info { get; init; }

    private Dictionary<string, string> ProductInfo {
        get { return _productInfo ??= Info.First(r => r.Key.Count(c => c == '.') == 3).Value; }
    }

    public decimal Tick {
        get { return _tick ??= Convert.ToDecimal(ProductInfo["TickSize"]); }
    }

    public int Decimals {
        get {
            if (_decimals != null) {
                return (int)_decimals;
            }
            
            var ticksPerPoint = Convert.ToInt32(ProductInfo["TicksPerPoint"]);
            
            var decimals = 0;
            while ((ticksPerPoint /= 10) >= 1) {
                decimals++;
            }
            
            return _decimals ??= decimals;
        }
    }
}