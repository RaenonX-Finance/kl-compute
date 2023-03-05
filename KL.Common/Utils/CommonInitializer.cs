using KL.Common.Controllers;

namespace KL.Common.Utils;


public static class CommonInitializer {
    public static async Task Initialize() {
        await Task.WhenAll(
            MongoManager.Initialize(),
            RedisHelper.TestConnection()
        );
    }
}