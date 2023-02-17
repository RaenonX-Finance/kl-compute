namespace KL.Touchance.Requests;


public record QueryInstrumentRequest : TcRequest {
    public required string SessionKey { get; init; }

    public required string Symbol { get; init; }
}