using KL.Common.Interfaces;

namespace KL.Common.Models;


public struct RedisHlcBarFromLastMeta : IPxBarHlcOnly {
    public required decimal High { get; init; }

    public required decimal Low { get; init; }

    public required decimal Close { get; init; }
}