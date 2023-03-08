using JetBrains.Annotations;
using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace KL.Common.Models;


public record SrLevelDataModel {
    [UsedImplicitly]
    public required SrLevelType Type { get; init; }

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

            if (diff < PxConfigController.Config.SrLevel.MinDiff) {
                return Array.Empty<decimal>();
            }

            return NumberHelper.DecreasingRangeWithStep(
                    Math.Min(LastClose, CurrentOpen),
                    Math.Min(LastClose, CurrentOpen) * (decimal)0.95,
                    -diff
                )
                .Concat(
                    NumberHelper.IncreasingRangeWithStep(
                        Math.Max(LastClose, CurrentOpen),
                        Math.Max(LastClose, CurrentOpen) * (decimal)1.05,
                        diff
                    )
                )
                .Order()
                .ToArray();
        }
    }
}