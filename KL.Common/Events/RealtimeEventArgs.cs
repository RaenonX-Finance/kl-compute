﻿using KL.Common.Models;

namespace KL.Common.Events;


public class RealtimeEventArgs : EventArgs {
    public required string Symbol { get; init; }

    public required PxRealtimeModel Data { get; init; }

    public DateTime? Timestamp { get; init; }

    public required bool IsTriggeredByHistory { get; init; }
}