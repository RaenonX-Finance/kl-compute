using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record HistorySourceConfigModel {
    [UsedImplicitly]
    public required double BatchUpdateDelayMs { get; init; }
}
