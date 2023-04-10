using JetBrains.Annotations;
using KL.Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KL.Common.Models;


public record HistoryDataModel {
    [UsedImplicitly]
    public ObjectId Id { get; init; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    [UsedImplicitly]
    public required DateTime Timestamp { get; init; }

    [UsedImplicitly]
    public required decimal Open { get; init; }

    [UsedImplicitly]
    public required decimal High { get; init; }

    [UsedImplicitly]
    public required decimal Low { get; init; }

    [UsedImplicitly]
    public required decimal Close { get; init; }

    [UsedImplicitly]
    public required int Volume { get; init; }

    [UsedImplicitly]
    public required HistoryInterval Interval { get; init; }

    [UsedImplicitly]
    public required long EpochSecond { get; init; }

    [UsedImplicitly]
    public required DateOnly MarketDate { get; init; }
}