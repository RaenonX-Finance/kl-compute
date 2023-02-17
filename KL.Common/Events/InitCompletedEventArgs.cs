using System.Collections.Immutable;
using KL.Common.Models.Config;

namespace KL.Common.Events;


public class InitCompletedEventArgs : EventArgs {
    public required ImmutableList<PxSourceConfigModel> SourcesInUse { get; init; }
}