using JetBrains.Annotations;
using KL.Common.Controllers;
using KL.Common.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace KL.Common.Models;


public record SrLevelDataModel {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required DateOnly LastDate { get; init; }

    [UsedImplicitly]
    public required decimal LastClose { get; init; }

    [UsedImplicitly]
    public required DateOnly CurrentDate { get; init; }

    [UsedImplicitly]
    public required decimal CurrentOpen { get; init; }

    [UsedImplicitly]
    [BsonElement]
    public decimal[] Levels {
        get {
            var diff = Math.Abs(LastClose - CurrentOpen);

            if (diff < PxConfigController.Config.Sources[Symbol].SrLevelMinDiff) {
                return Array.Empty<decimal>();
            }

            return NumberHelper.DecreasingRangeWithStep(
                    Math.Min(LastClose, CurrentOpen),
                    Math.Min(LastClose, CurrentOpen) * 0.95m,
                    -diff
                )
                .Concat(
                    NumberHelper.IncreasingRangeWithStep(
                        Math.Max(LastClose, CurrentOpen),
                        Math.Max(LastClose, CurrentOpen) * 1.05m,
                        diff
                    )
                )
                .Order()
                .ToArray();
        }
    }
}