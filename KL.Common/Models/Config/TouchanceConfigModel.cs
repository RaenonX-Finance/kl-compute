using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record TouchanceConfigModel {
    [UsedImplicitly]

    public required int ZmqPort { get; init; }

    [UsedImplicitly]

    public required int LoginTimeout { get; init; }
}