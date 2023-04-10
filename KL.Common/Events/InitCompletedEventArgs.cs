using KL.Common.Models.Config;

namespace KL.Common.Events;


public class InitCompletedEventArgs : EventArgs {
    public required IList<PxSourceConfigModel> Sources { get; init; }

    public required OnUpdate OnUpdate { get; init; }
}