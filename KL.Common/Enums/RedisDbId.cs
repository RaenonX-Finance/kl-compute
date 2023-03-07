using JetBrains.Annotations;

namespace KL.Common.Enums;


public enum RedisDbId {
    LastPxAndMomentum = 0,

    // Added to note that the database is occupied
    [UsedImplicitly]
    SocketIoCluster = 8
}