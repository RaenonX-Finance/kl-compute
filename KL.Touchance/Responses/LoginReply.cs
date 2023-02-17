using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace KL.Touchance.Responses;


public record LoginReply : TcReply {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int SubPort { get; init; }
}