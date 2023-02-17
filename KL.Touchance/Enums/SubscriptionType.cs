namespace KL.Touchance.Enums;


public static class SubscriptionType {
    public const string Ping = "PING";
    public const string Realtime = "REALTIME";
    public const string HistoryMinute = "1K";
    public const string HistoryDaily = "DK";
    public const string HistoryTick = "TICKS";
    public const string HistoryUnsubscribe = "UNSUBQUOTE";
    public const string MinuteChange = "SYSTEMTIME";
}