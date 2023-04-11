using JetBrains.Annotations;
using KL.Common.Enums;
using KL.Common.Models.Config;
using MongoDB.Bson;

namespace KL.Common.Models;


public record PxConfigModel {
    [UsedImplicitly]
    public ObjectId Id { get; init; }

    [UsedImplicitly]
    public required EmaPairConfigModel EmaNet { get; init; }

    [UsedImplicitly]
    public required EmaPairConfigModel[] EmaStrongSr { get; init; }

    [UsedImplicitly]
    public required CandleDirectionConfigModel CandleDirection { get; init; }

    [UsedImplicitly]
    public required SrLevelConfigModel SrLevel { get; init; }

    [UsedImplicitly]
    public required DataPeriodModel[] Periods { get; init; }

    [UsedImplicitly]
    public required HistorySubscriptionConfigModel HistorySubscription { get; init; }

    [UsedImplicitly]
    public required Dictionary<ProductCategory, SrLevelTimingModel> SrLevelTimingMap { get; init; }

    [UsedImplicitly]
    public required Dictionary<ProductCategory, MarketSessionModel[]> MarketSessionMap { get; init; }

    [UsedImplicitly]
    public required Dictionary<ProductCategory, MarketDateCutoffModel> MarketDateCutoffMap { get; init; }

    [UsedImplicitly]
    public required Dictionary<HistoryInterval, int> InitDataBacktrackDays { get; init; }

    [UsedImplicitly]
    public required PxCacheConfigModel Cache { get; init; }

    [UsedImplicitly]
    // Storing as `Dictionary` for better setting update
    public required IDictionary<string, PxSourceConfigModel> Sources { get; init; }

    public IEnumerable<PxSourceConfigModel> SourceList => Sources.Values;

    public static PxConfigModel GenerateDefault() {
        return new PxConfigModel {
            EmaNet = new EmaPairConfigModel { Fast = 10, Slow = 34 },
            CandleDirection = new CandleDirectionConfigModel { Fast = 20, Slow = 300, Signal = 15 },
            EmaStrongSr = new EmaPairConfigModel[] {
                new() {
                    Fast = 144,
                    Slow = 169
                },
                new() {
                    Fast = 576,
                    Slow = 676
                }
            },
            SrLevel = new SrLevelConfigModel { PairCount = 5 },
            Periods = new DataPeriodModel[] {
                new() {
                    PeriodMin = 1,
                    Name = "1"
                },
                new() {
                    PeriodMin = 5,
                    Name = "5"
                },
                new() {
                    PeriodMin = 15,
                    Name = "15"
                },
                new() {
                    PeriodMin = 60,
                    Name = "60"
                }
            },
            HistorySubscription = new HistorySubscriptionConfigModel {
                StoreLimit = 2,
                InitialBufferHrs = 1
            },
            SrLevelTimingMap = Enum.GetValues<ProductCategory>()
                .ToDictionary(r => r, SrLevelTimingModel.GenerateDefault),
            MarketSessionMap = Enum.GetValues<ProductCategory>()
                .ToDictionary(r => r, MarketSessionModel.GenerateDefault),
            MarketDateCutoffMap = Enum.GetValues<ProductCategory>()
                .ToDictionary(r => r, MarketDateCutoffModel.GenerateDefault),
            InitDataBacktrackDays = new Dictionary<HistoryInterval, int> {
                { HistoryInterval.Minute, 45 },
                { HistoryInterval.Daily, 360 }
            },
            Cache = new PxCacheConfigModel {
                InitCount = 70, // Calculating momentum only needs at most 50 bars of 1K (10 MA of 5 min)
                UpdateCount = 2, // 2nd last might get correction
                MarketUpdateGapMs = 333
            },
            Sources = PxSourceConfigModel.GenerateDefault()
        };
    }
}