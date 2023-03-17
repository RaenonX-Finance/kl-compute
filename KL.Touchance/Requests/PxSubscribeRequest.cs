using KL.Touchance.Enums;

namespace KL.Touchance.Requests;


public record PxSubscribeRequest<T> : PxRequest<T> where T : PxRequestParams {
    public override string Request => RequestType.PxSubscribe;
}