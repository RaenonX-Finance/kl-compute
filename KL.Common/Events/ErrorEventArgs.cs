namespace KL.Common.Events;


public class PxErrorEventArgs : EventArgs {
    public required string Message { get; init; }
}