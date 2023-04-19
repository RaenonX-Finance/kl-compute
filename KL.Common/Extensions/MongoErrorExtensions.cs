using MongoDB.Driver;

namespace KL.Common.Extensions;


public static class MongoErrorExtensions {
    public static bool IsWriteConflictError(this MongoCommandException exception) {
        // https://github.com/mongodb/mongo/blob/master/src/mongo/base/error_codes.yml
        return exception.Code == 112;
    }
}