using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record DataPeriodModel {
    [UsedImplicitly]
    public required int PeriodMin { get; init; }

    [UsedImplicitly]
    public required string Name { get; init; }
}