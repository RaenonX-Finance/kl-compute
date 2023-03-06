using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record RestConfigModel {
    [UsedImplicitly]
    public required int ApiPort { get; init; }
}