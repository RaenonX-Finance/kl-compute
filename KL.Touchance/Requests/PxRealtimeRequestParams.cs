using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public record PxRealtimeRequestParams : PxRequestParams {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public override string SubDataType => "REALTIME";
}
