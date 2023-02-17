using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace KL.Touchance.Requests;


public record PxHistoryDataRequestParams : PxHistoryRequestParams {
    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public required int QryIndex { get; init; }
}

public record PxHistoryDataRequest : PxHistoryRequest<PxHistoryDataRequestParams>;