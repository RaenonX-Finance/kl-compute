using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public abstract record PxRequestParams {
    [UsedImplicitly]
    public abstract string SubDataType { get; }
}

public abstract record PxRequest<T> : TcRequest where T : PxRequestParams {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    public required T Param { get; init; }
}