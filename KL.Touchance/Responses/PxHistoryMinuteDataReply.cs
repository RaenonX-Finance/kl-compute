using KL.Common.Enums;

namespace KL.Touchance.Responses;


public record PxHistoryMinuteDataReply : PxHistoryDataReply {
    public override HistoryInterval Interval => HistoryInterval.Minute;
}