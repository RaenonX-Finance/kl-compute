using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record DatabaseConfigModel {
    [UsedImplicitly]
    public required string MongoUrl { get; init; }

    [UsedImplicitly]
    public required string RedisAddress { get; init; }
}