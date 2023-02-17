using KL.Common.Controllers;
using KL.Common.Extensions;
using KL.Common.Interfaces;

namespace KL.PxParse.Controllers;


public static class PxCacheController {
    private static readonly Dictionary<string, SortedDictionary<DateTime, decimal>> LastCloses = new();

    public static void Create(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        LastCloses[symbol] = entries
            .TakeLast(PxConfigController.Config.Cache.InitCount)
            .ToSortedDictionary(r => r.Timestamp, r => r.Close);
    }

    public static void Update(string symbol, IEnumerable<IHistoryDataEntry> entries) {
        foreach (var entry in entries.TakeLast(PxConfigController.Config.Cache.UpdateCount)) {
            LastCloses[symbol][entry.Timestamp] = entry.Close;
        }
    }

    public static void CreateNewBar(DateTime timestamp) {
        foreach (var symbol in LastCloses.Keys) {
            LastCloses[symbol].Add(timestamp, LastCloses[symbol].Last().Value);
        }
    }
}