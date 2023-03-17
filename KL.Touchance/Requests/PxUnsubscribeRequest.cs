using KL.Touchance.Enums;

namespace KL.Touchance.Requests;


public record PxUnsubscribeRequest<T> : PxRequest<T> where T : PxRequestParams {
    public override string Request => RequestType.PxUnsubscribe;
}