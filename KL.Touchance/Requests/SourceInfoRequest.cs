using JetBrains.Annotations;
using KL.Touchance.Enums;

namespace KL.Touchance.Requests;


public record SourceInfoRequest : TcRequest {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    public required string Symbol { get; init; }

    public override string Request => RequestType.SourceInfo;
}