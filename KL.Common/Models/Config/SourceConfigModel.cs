using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record SourceConfigModel {
    [UsedImplicitly]
    public required TouchanceConfigModel Touchance { get; init; }
}
