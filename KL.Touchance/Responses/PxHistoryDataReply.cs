using System.Text.Json.Serialization;
using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Interfaces;
using KL.Touchance.Enums;
using KL.Touchance.Extensions;
using KL.Touchance.Interfaces;

namespace KL.Touchance.Responses;


[UsedImplicitly]
public record PxHistoryEntry : ITimestamp, IHistoryDataEntry {
    private DateTime? _timestamp;

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int UpTick { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int UpVolume { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int DownTick { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int DownVolume { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int UnchVolume { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int QryIndex { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required decimal Open { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required decimal High { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required decimal Low { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required decimal Close { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int Volume { get; init; }

    // Touchance "Time" column return the minute in the future, not the current (02:50 will return 0251)
    public DateTime Timestamp => _timestamp ??= this.GetTimestamp(-1);

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int Date { get; init; }

    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public required int Time { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "DataType")]
[JsonDerivedType(typeof(PxHistoryMinuteDataReply), HistoryDataType.Minute)]
[JsonDerivedType(typeof(PxHistoryDailyDataReply), HistoryDataType.Daily)]
public abstract record PxHistoryDataReply : TcReply {
    [UsedImplicitly]
    public abstract HistoryInterval Interval { get; }

    [UsedImplicitly]
    public required PxHistoryEntry[] HisData { get; init; }

    public int LastQueryIndex => HisData[^1].QryIndex;
}