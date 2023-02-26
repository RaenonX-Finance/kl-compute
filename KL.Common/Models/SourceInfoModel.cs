using JetBrains.Annotations;

namespace KL.Common.Models;


public record SourceInfoModel {
    [UsedImplicitly]
    public required string Symbol { get; init; }

    [UsedImplicitly]
    public required decimal MinTick { get; init; }
    
    [UsedImplicitly]
    public required int Decimals { get; init; }
}
