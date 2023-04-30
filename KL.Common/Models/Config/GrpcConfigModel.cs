using JetBrains.Annotations;

namespace KL.Common.Models.Config;


public record GrpcTimeoutConfig {
    [UsedImplicitly]
    public required int Default { get; init; }

    [UsedImplicitly]
    public required int CalcAll { get; init; }
}

public record GrpcConfigModel {
    [UsedImplicitly]
    public required int CalcPort { get; init; }
    
    [UsedImplicitly]
    public required int PxParsePort { get; init; }
    
    [UsedImplicitly]
    public required int PxInfoPort { get; init; }

    [UsedImplicitly]
    public required int SysPort { get; init; }
    
    [UsedImplicitly]
    public required GrpcTimeoutConfig Timeout { get; init; }
}