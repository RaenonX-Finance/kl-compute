using KL.Common.Enums;
using KL.Common.Extensions;

namespace KL.Common.Interfaces;


public interface IHistoryMetadata {
    public string Symbol { get; }

    public HistoryInterval Interval { get; }

    public DateTime Start { get; }

    public DateTime End { get; }

    public string ToIdentifier() {
        return $"<{Symbol}-{Interval.ToString()[0]}-{Start.ToShortIso8601()}-{End.ToShortIso8601()}>";
    }
}