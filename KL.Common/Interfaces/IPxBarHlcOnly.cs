namespace KL.Common.Interfaces;


public interface IPxBarHlcOnly {
    public decimal High { get; }

    public decimal Low { get; }

    public decimal Close { get; }
}