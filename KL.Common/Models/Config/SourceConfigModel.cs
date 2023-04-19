using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record SourceConfigModel {
    [UsedImplicitly]
    public required TouchanceConfigModel Touchance { get; init; }

    [UsedImplicitly]
    public required CommonSourceConfigModel Common { get; init; }
}
