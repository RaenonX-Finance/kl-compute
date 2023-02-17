namespace KL.Common.Utils;


public static class TaskHelper {
    public static void FireAndForget(
        Func<Task?> function,
        Action<Exception?> onException,
        CancellationToken cancellationToken
    ) {
        Task
            .Run(function, cancellationToken)
            .ContinueWith(
                t => {
                    // `t.Exception?.Flatten().InnerExceptions` guaranteed not null
                    // by `TaskContinuationOptions.OnlyOnFaulted`
                    foreach (var exception in t.Exception?.Flatten().InnerExceptions!) {
                        onException(exception);
                    }
                },
                TaskContinuationOptions.OnlyOnFaulted
            );
    }
}