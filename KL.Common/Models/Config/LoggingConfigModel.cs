using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record LoggingConfigModel {
    [UsedImplicitly]
    public string? OutputDirectory { get; init; }

    [UsedImplicitly]
    public string? NewRelicApiKey { get; init; }
}