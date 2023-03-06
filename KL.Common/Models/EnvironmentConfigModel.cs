using JetBrains.Annotations;
using KL.Common.Models.Config;

namespace KL.Common.Models;


public record EnvironmentConfigModel {
    [UsedImplicitly]
    public required DatabaseConfigModel Database { get; init; }

    [UsedImplicitly]
    public required LoggingConfigModel Logging { get; init; }

    [UsedImplicitly]
    public required SourceConfigModel Source { get; init; }

    [UsedImplicitly]
    public required GrpcConfigModel Grpc { get; init; }

    [UsedImplicitly]
    public required RestConfigModel Rest { get; init; }
}