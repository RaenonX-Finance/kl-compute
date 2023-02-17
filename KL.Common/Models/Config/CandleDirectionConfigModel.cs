using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record CandleDirectionConfigModel : EmaPairConfigModel {
    [UsedImplicitly]
    public required int Signal { get; init; }
}