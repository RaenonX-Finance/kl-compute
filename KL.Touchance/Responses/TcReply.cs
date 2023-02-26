using System.Text.Json.Serialization;
using KL.Touchance.Enums;

namespace KL.Touchance.Responses;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "Reply")]
[JsonDerivedType(typeof(LoginReply), RequestType.Login)]
[JsonDerivedType(typeof(PongReply), RequestType.Pong)]
[JsonDerivedType(typeof(SourceInfoReply), RequestType.SourceInfo)]
[JsonDerivedType(typeof(PxHistoryHandshakeReply), RequestType.HistoryHandshake)]
[JsonDerivedType(typeof(PxHistoryUnsubscribeReply), RequestType.HistoryUnsubscribe)]
public abstract record TcReply;