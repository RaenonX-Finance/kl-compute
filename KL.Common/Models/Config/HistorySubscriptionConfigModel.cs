namespace KL.Common.Models.Config;


public record HistorySubscriptionConfigModel {
    public required int StoreLimit { get; init; }

    public required int InitialBufferHrs { get; init; }
}