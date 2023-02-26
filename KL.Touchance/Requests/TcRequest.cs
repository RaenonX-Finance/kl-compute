using System.Text.Json;
using System.Text.Json.Serialization;
using KL.Touchance.Enums;
using KL.Touchance.Utils;

namespace KL.Touchance.Requests;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "Request")]
[JsonDerivedType(typeof(LoginRequest), RequestType.Login)]
[JsonDerivedType(typeof(PongRequest), RequestType.Pong)]
[JsonDerivedType(typeof(SourceInfoRequest), RequestType.SourceInfo)]
[JsonDerivedType(typeof(PxHistoryHandshakeRequest), RequestType.HistoryHandshake)]
[JsonDerivedType(typeof(PxHistoryDataRequest), RequestType.HistoryData)]
[JsonDerivedType(typeof(PxHistoryUnsubscribeRequest), RequestType.HistoryUnsubscribe)]
public abstract record TcRequest {
    public string ToJson() {
        return JsonSerializer.Serialize(this, JsonHelper.SerializingOptions);
    }
}