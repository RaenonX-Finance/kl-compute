using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record TouchanceTimeoutModel {
    [UsedImplicitly]
    public required int Login { get; init; }

    [UsedImplicitly]
    public required int Request { get; init; }
}