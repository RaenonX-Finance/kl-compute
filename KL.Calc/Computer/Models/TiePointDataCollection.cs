namespace KL.Calc.Computer.Models;


public class TiePointDataCollection {
    public required IList<decimal> MarketDateHigh { get; init; }

    public required IList<decimal> MarketDateLow { get; init; }

    public required IList<decimal> TiePoint { get; init; }
}