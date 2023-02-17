namespace KL.Proto.Types;


public partial class DecimalValue {
    private const decimal NanoFactor = 1_000_000_000;

    private readonly long _units;
    private readonly long _nanos;

    public DecimalValue(long units, int nanos) {
        _units = units;
        _nanos = nanos;
    }

    public static implicit operator decimal(DecimalValue grpcDecimal) {
        return grpcDecimal._units + grpcDecimal._nanos / NanoFactor;
    }

    public static implicit operator DecimalValue(decimal value) {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue(units, nanos);
    }
}