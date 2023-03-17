using System.Text.Json.Serialization;
using KL.Touchance.Enums;

namespace KL.Touchance.Subscriptions;


[JsonPolymorphic(TypeDiscriminatorPropertyName = "DataType")]
[JsonDerivedType(typeof(PingMessage), SubscriptionType.Ping)]
[JsonDerivedType(typeof(PxHistoryMinuteReadyMessage), SubscriptionType.HistoryMinute)]
[JsonDerivedType(typeof(PxHistoryDailyReadyMessage), SubscriptionType.HistoryDaily)]
[JsonDerivedType(typeof(PxRealtimeMessage), SubscriptionType.Realtime)]
[JsonDerivedType(typeof(SymbolClearMessage), SubscriptionType.SymbolClear)]
[JsonDerivedType(typeof(MinuteChangeMessage), SubscriptionType.MinuteChange)]
public abstract record TcSubscription;