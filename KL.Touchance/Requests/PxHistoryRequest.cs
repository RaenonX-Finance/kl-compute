using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public record PxHistoryRequestParams {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required string SubDataType { get; init; }

    [UsedImplicitly]
    public required string StartTime { get; init; }

    [UsedImplicitly]
    public required string EndTime { get; init; }
}

public record PxHistoryRequest<T> : TcRequest where T : PxHistoryRequestParams {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    public required T Param { get; init; }
}