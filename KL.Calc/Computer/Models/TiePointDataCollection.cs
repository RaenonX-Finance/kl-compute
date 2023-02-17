using System.Collections.Immutable;

namespace KL.Calc.Computer.Models;


public class TiePointDataCollection {
    public required IImmutableList<decimal> MarketDateHigh { get; init; }

    public required IImmutableList<decimal> MarketDateLow { get; init; }

    public required IImmutableList<decimal> TiePoint { get; init; }
}