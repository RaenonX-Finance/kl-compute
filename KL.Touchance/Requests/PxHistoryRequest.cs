using JetBrains.Annotations;
using KL.Common.Enums;

namespace KL.Touchance.Requests;


public abstract record PxHistoryRequestParams : PxRequestParams {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required string StartTime { get; init; }

    [UsedImplicitly]
    public required string EndTime { get; init; }

    [UsedImplicitly]
    public required HistoryInterval Interval { get; init; }
}

public abstract record PxHistoryRequest<T> : PxRequest<T> where T : PxHistoryRequestParams;