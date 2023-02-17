using KL.Common.Enums;

namespace KL.Touchance.Subscriptions;


public record PxHistoryDailyReadyMessage : PxHistoryReadyMessage {
    public override HistoryInterval Interval => HistoryInterval.Daily;
}