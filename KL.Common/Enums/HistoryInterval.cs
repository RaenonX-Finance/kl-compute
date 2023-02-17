using System.Runtime.Serialization;

namespace KL.Common.Enums;


public enum HistoryInterval {
    [EnumMember(Value = "1K")]
    Minute,

    [EnumMember(Value = "DK")]
    Daily
}

public static class HistoryIntervalExtensions {
    public static HistoryInterval GetHistoryInterval(this int periodMin) {
        return periodMin < 1440 ? HistoryInterval.Minute : HistoryInterval.Daily;
    }
}