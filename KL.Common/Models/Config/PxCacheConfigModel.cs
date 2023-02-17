namespace KL.Common.Models.Config;


public record PxCacheConfigModel {
    public required int InitCount { get; init; }

    public required int UpdateCount { get; init; }
}