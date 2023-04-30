using KL.GrpcCaller.Enums;

namespace KL.GrpcCaller.Extensions;


public static class ConfigurationExtensions {
    public static WorkerAction GetAction(this IConfiguration configuration) {
        var actionValue = configuration[ConfigKeys.Action];

        if (actionValue is null) {
            throw new InvalidCastException(
                $"Specify actions as `--{ConfigKeys.Action}={WorkerAction.Subscribe.ToString()}`. "
                + $"Valid values are [{string.Join(", ", Enum.GetNames(typeof(WorkerAction)))}]"
            );
        }

        var parsed = Enum.TryParse<WorkerAction>(actionValue, ignoreCase: true, out var action);

        if (!parsed) {
            throw new InvalidCastException(
                $"Invalid action: [{actionValue}], "
                + $"valid values are [{string.Join(", ", Enum.GetNames(typeof(WorkerAction)))}]"
            );
        }

        return action;
    }

    public static IEnumerable<string> GetSymbols(this IConfiguration configuration) {
        var symbols = configuration[ConfigKeys.Symbols]?.Split(",");

        if (symbols is null) {
            throw new InvalidDataException(
                $"Symbols should be provided in command line args as `--{ConfigKeys.Symbols}=NQ,ES`"
            );
        }

        return symbols.Select(r => r.Trim());
    }
}