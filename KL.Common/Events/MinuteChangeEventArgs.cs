using KL.Common.Extensions;

namespace KL.Common.Events;


public class MinuteChangeEventArgs : EventArgs {
    public required string Symbol { get; init; }
    
    public required DateTime Timestamp { get; init; }

    public long EpochSecond => Timestamp.ToEpochSeconds();
}