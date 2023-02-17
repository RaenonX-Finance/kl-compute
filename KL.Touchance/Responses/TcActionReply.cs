using JetBrains.Annotations;

namespace KL.Touchance.Responses;


public abstract record TcActionReply : TcReply {
    [UsedImplicitly]
    public required string Success { get; init; }
}