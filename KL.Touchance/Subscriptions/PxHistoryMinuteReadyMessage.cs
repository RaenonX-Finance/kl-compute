using KL.Common.Enums;

namespace KL.Touchance.Subscriptions;


public record PxHistoryMinuteReadyMessage : PxHistoryReadyMessage {
    public override HistoryInterval Interval => HistoryInterval.Minute;
}