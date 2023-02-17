namespace KL.Touchance.Enums;


public static class RequestType {
    public const string Login = "LOGIN";
    public const string Pong = "PONG";
    public const string HistoryHandshake = "SUBQUOTE";
    public const string HistoryUnsubscribe = "UNSUBQUOTE";
    public const string HistoryData = "GETHISDATA";
    public const string QueryInstrument = "QUERYINSTRUMENTINFO";
}