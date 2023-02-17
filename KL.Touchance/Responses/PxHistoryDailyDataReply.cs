using KL.Common.Enums;

namespace KL.Touchance.Responses;


public record PxHistoryDailyDataReply : PxHistoryDataReply {
    public override HistoryInterval Interval => HistoryInterval.Daily;
}