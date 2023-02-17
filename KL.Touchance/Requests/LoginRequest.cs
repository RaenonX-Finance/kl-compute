using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public record LoginRequestParams {
    [UsedImplicitly]
    public required string SystemName { get; init; }

    [UsedImplicitly]
    public required string ServiceKey { get; init; }
}

public record LoginRequest : TcRequest {
    [UsedImplicitly]
    public required LoginRequestParams Param { get; init; }
}