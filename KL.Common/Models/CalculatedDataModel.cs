using JetBrains.Annotations;
using KL.Common.Interfaces;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KL.Common.Models;


public class CalculatedDataModel : IGroupedHistoryDataModel {
    [UsedImplicitly]
    public ObjectId Id { get; init; }

    [UsedImplicitly]
    public required int PeriodMin { get; init; }

    [UsedImplicitly]
    public required decimal Diff { get; set; }

    [UsedImplicitly]
    public required decimal MarketDateHigh { get; set; }

    [UsedImplicitly]
    public required decimal MarketDateLow { get; set; }

    [UsedImplicitly]
    public required decimal TiePoint { get; set; }

    [UsedImplicitly]
    public required double? MacdSignal { get; set; }

    [UsedImplicitly]
    public required int CandleDirection { get; set; }

    [UsedImplicitly]
    public required Dictionary<int, double?> Ema { get; init; }

    [UsedImplicitly]
    public required string Symbol { get; init; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [UsedImplicitly]
    public required DateTime Date { get; init; }

    [UsedImplicitly]
    public required decimal Open { get; init; }

    [UsedImplicitly]
    public required decimal High { get; init; }

    [UsedImplicitly]
    public required decimal Low { get; init; }

    [UsedImplicitly]
    public required decimal Close { get; init; }

    [UsedImplicitly]
    public required decimal Volume { get; init; }

    [UsedImplicitly]
    public required DateOnly MarketDate { get; init; }

    [UsedImplicitly]
    public required long EpochSecond { get; init; }
}