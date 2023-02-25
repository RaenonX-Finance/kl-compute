namespace KL.Common.Controllers;


public static class MongoManager {
    public static async Task Initialize() {
        await Task.WhenAll(MongoIndexManager.Initialize());
    }
}