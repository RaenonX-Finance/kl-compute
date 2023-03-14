using KL.Common.Events;
using KL.Common.Utils;
using Serilog;

namespace KL.Common.Controllers;


public abstract class PxParseClient {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(PxParseClient));

    protected readonly CancellationToken CancellationToken;

    private readonly bool _triggerRealtimeOnHistory;

    protected PxParseClient(bool triggerRealtimeOnHistory, CancellationToken cancellationToken) {
        _triggerRealtimeOnHistory = triggerRealtimeOnHistory;

        CancellationToken = cancellationToken;
    }

    private void InvokeAsyncEvent<TEventArgs>(
        AsyncEventHandler<TEventArgs>? eventHandler,
        TEventArgs e,
        Action<TEventArgs>? onHandled = null
    )
        where TEventArgs : EventArgs {
        if (eventHandler is null) {
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
        if (InitCompletedEvent is null) {
            return;
        }

        await InitCompletedEvent(this, e);
    }

    public event AsyncEventHandler<RealtimeEventArgs>? RealtimeDataUpdatedEventAsync;

    private void OnRealtimeDataUpdated(RealtimeEventArgs e) {
        InvokeAsyncEvent(RealtimeDataUpdatedEventAsync, e);
    }

    public event AsyncEventHandler<HistoryEventArgs>? HistoryDataUpdatedEventAsync;

    public async Task OnHistoryDataUpdated(HistoryEventArgs e) {
        if (e is { IsSubscription: true, Data.Count: 0 }) {
            Log.Warning(
                "[{Identifier}] Skipped processing history data - is subscription but no data",
                e.Metadata.ToIdentifier()
            );
            return;
        }

        if (HistoryDataUpdatedEventAsync != null) {
            await HistoryDataUpdatedEventAsync.Invoke(this, e);
            OnHistoryDataUpdatedCompleted(e);
        }


        if (_triggerRealtimeOnHistory && e.IsSubscription) {
            OnRealtimeDataUpdated(e.ToRealtimeEventArgs());
        }
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