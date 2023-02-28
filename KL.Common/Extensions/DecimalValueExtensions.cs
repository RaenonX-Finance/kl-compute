using KL.Proto;

namespace KL.Common.Extensions;


public static class DecimalValueExtensions {
    private const decimal NanoFactor = 1_000_000_000;

    public static decimal ToDecimal(this DecimalValue grpcDecimal) {
        return grpcDecimal.Units + grpcDecimal.Nanos / NanoFactor;
    }

    public static DecimalValue ToGrpcDecimal(this decimal value) {
        var units = decimal.ToInt64(value);

        return new DecimalValue {
            Units = units,
            Nanos = decimal.ToInt32((value - units) * NanoFactor)
        };
    }
}