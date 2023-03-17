using JetBrains.Annotations;

namespace KL.Touchance.Responses;


public record LoginReply : TcReply {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    public required int SubPort { get; init; }
}