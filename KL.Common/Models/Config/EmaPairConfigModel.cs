using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record EmaPairConfigModel {
    [UsedImplicitly]
    public required int Fast { get; init; }

    [UsedImplicitly]
    public required int Slow { get; init; }

    public int[] EmaPeriods {
        get { return new[] { Fast, Slow }; }
    }
}