using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record SrLevelConfigModel {
    [UsedImplicitly]
    public required int MinDiff { get; init; }
    
    [UsedImplicitly]
    public required int PairCount { get; init; }
}