using System.Text.Json.Serialization;
using JetBrains.Annotations;
using KL.Touchance.Enums;
using KL.Touchance.Extensions;

namespace KL.Touchance.Requests;


public record PxHistoryDataRequestParams : PxHistoryRequestParams {
    [UsedImplicitly]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
    public required int QryIndex { get; init; }

    public override string SubDataType => Interval.GetTouchanceType();
}

public record PxHistoryDataRequest : PxHistoryRequest<PxHistoryDataRequestParams> {
    public override string Request => RequestType.HistoryData;
}