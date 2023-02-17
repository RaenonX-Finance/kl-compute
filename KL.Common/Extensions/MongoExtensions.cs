using MongoDB.Driver;

namespace KL.Common.Extensions;


public static class MongoExtensions {
    public static string GetSessionId(this IClientSessionHandle session) {
        return session.WrappedCoreSession.Id["id"].AsGuid.ToString();
    }
}