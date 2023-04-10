using KL.Common.Controllers;
using KL.Common.Enums;
using KL.Common.Extensions;
using KL.Common.Models;

namespace KL.Common.Interfaces;


public interface IHistoryDataEntry {
    public DateTime Timestamp { get; }

    public decimal Open { get; }

    public decimal High { get; }

    public decimal Low { get; }

    public decimal Close { get; }

    public int Volume { get; }

    public HistoryDataModel ToHistoryDataModel(string symbol, HistoryInterval interval) {
        var epochSecond = Timestamp.ToEpochSeconds();
        var category = PxConfigController.Config.Sources[symbol].ProductCategory;

        return new HistoryDataModel {
            Timestamp = Timestamp,
            Open = Open,
            High = High,
            Low = Low,
            Close = Close,
            Volume = Volume,
            Interval = interval,
            EpochSecond = epochSecond,
            MarketDate = Timestamp.ToMarketDate(PxConfigController.Config.MarketDateCutoffMap[category])
        };
    }
}