using JetBrains.Annotations;

namespace KL.Common.Models;


public record DatabaseConfig {
    [UsedImplicitly]
    public required string MongoUrl { get; init; }

    [UsedImplicitly]
    public required string RedisAddress { get; init; }
}

public record LoggingConfig {
    [UsedImplicitly]
    public string? OutputDirectory { get; init; }

    [UsedImplicitly]
    public string? NewRelicApiKey { get; init; }
}

public record GrpcConfig {
    [UsedImplicitly]
    public required int CalcPort { get; init; }

    [UsedImplicitly]
    public required int SysPort { get; init; }
}

public record RestConfig {
    [UsedImplicitly]
    public required int ApiPort { get; init; }
}

public record EnvironmentConfigModel {
    [UsedImplicitly]
    public required DatabaseConfig Database { get; init; }

    [UsedImplicitly]
    public required LoggingConfig Logging { get; init; }

    [UsedImplicitly]
    public required GrpcConfig Grpc { get; init; }

    [UsedImplicitly]
    public required RestConfig Rest { get; init; }
}