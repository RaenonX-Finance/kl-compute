namespace KL.Common.Utils; 


public delegate Task OnDelayReached<in T>(T args);

public class DelayedOperation<T> {
    private readonly OnDelayReached<T> _onDelayReached;
    
    private readonly TimeSpan _delay;

    private Task? _pendingTask;
    
    private T? _onDelayReachedArgs;

    public DelayedOperation(OnDelayReached<T> onDelayReached, TimeSpan delay) {
        _onDelayReached = onDelayReached;
        _delay = delay;
    }

    public void UpdateArgs(T args) {
        _onDelayReachedArgs = args;

        if (_pendingTask is null || _pendingTask.IsCompleted) {
            _pendingTask = Task.Delay(_delay).ContinueWith(_ => _onDelayReached(_onDelayReachedArgs));
        }
    }
}