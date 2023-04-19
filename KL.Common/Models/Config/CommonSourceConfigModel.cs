using JetBrains.Annotations;

namespace KL.Common.Models.Config; 


public record CommonSourceConfigModel {
    [UsedImplicitly]
    public required HistorySourceConfigModel History { get; init; }
}
