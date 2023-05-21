using Serilog;

namespace KL.Common.Utils;


public static class AppSingletonEnforcer {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(AppSingletonEnforcer));

    public static void Enforce(string appName) {
        new Mutex(true, appName, out var isMutexCreated);

        if (isMutexCreated) {
            return;
        }

        Log.Fatal("{AppName} is already running!", appName);
        Environment.Exit(1);
    }
}