using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public record PongRequest : TcRequest {
    [UsedImplicitly]
    public required string SessionKey { get; init; }

    [UsedImplicitly]
    [JsonPropertyName("ID")]
    public required string Id { get; init; }
}