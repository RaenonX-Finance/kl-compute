using KL.Common.Utils;
using StackExchange.Redis;

namespace KL.Common.Controllers;


public static class RedisConst {
    public static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect(
        new ConfigurationOptions {
            EndPoints = { EnvironmentConfigHelper.Config.Database.RedisAddress },
            AllowAdmin = true
        }
    );
}
