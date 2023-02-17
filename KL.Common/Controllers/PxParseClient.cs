using KL.Common.Events;
using KL.Common.Utils;
using Serilog;

namespace KL.Common.Controllers;


public abstract class PxParseClient {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PxParseClient));

    protected readonly CancellationToken CancellationToken;

    protected PxParseClient(CancellationToken cancellationToken) {
        CancellationToken = cancellationToken;
    }

    private void InvokeAsyncEvent<TEventArgs>(
        AsyncEventHandler<TEventArgs>? eventHandler,
        TEventArgs e,
        Action<TEventArgs>? onHandled = null
    )
        where TEventArgs : EventArgs {
        if (eventHandler == null) {
            return;
        }

        TaskHelper.FireAndForget(
            async () => {
                await eventHandler.Invoke(this, e);
                onHandled?.Invoke(e);
            },
            exception => Log.Error(
                exception,
                "Exception on event {Event}",
                eventHandler.GetType()
            ),
            CancellationToken
        );
    }

    public event AsyncEventHandler<InitCompletedEventArgs>? InitCompletedEvent;

    protected async Task OnInitCompleted(InitCompletedEventArgs e) {
        if (InitCompletedEvent == null) {
            return;
        }

        await InitCompletedEvent(this, e);
    }

    public event AsyncEventHandler<HistoryEventArgs>? HistoryDataUpdatedEventAsync;

    public void OnHistoryDataUpdated(HistoryEventArgs e) {
        InvokeAsyncEvent(HistoryDataUpdatedEventAsync, e, OnHistoryDataUpdatedCompleted);
    }

    protected virtual void OnHistoryDataUpdatedCompleted(HistoryEventArgs e) { }

    public event AsyncEventHandler<MinuteChangeEventArgs>? MinuteChangeEventAsync;

    public void OnMinuteChanged(MinuteChangeEventArgs e) {
        InvokeAsyncEvent(MinuteChangeEventAsync, e);
    }

    public event AsyncEventHandler<PxErrorEventArgs>? PxErrorEventAsync;

    public void OnPxError(PxErrorEventArgs e) {
        InvokeAsyncEvent(PxErrorEventAsync, e);
    }
}