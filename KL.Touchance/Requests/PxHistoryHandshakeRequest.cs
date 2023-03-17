using KL.Touchance.Extensions;

namespace KL.Touchance.Requests;


public record PxHistoryHandshakeRequestParams : PxHistoryRequestParams {

    public override string SubDataType => Interval.GetTouchanceType();
}

public record PxHistoryHandshakeRequest : PxSubscribeRequest<PxHistoryHandshakeRequestParams>;