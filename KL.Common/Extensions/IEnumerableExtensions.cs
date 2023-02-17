using System.Numerics;

namespace KL.Common.Extensions;


public static class EnumerableExtensions {
    public static IEnumerable<TNumber> GroupedCumulativeMax<T, TKey, TNumber>(
        this IEnumerable<T> sequence,
        Func<T, TKey> groupKey,
        Func<T, TNumber> numericTarget,
        Dictionary<TKey, TNumber>? initialRecord = default
    ) where TNumber : INumber<TNumber> where TKey : notnull {
        var record = initialRecord ?? new Dictionary<TKey, TNumber>();

        foreach (var item in sequence) {
            var key = groupKey(item);
            var currentMax = numericTarget(item);

            if (record.TryGetValue(key, out var recordMax) && recordMax > currentMax) {
                yield return recordMax;
            }

            record[key] = currentMax;
            yield return currentMax;
        }
    }

    public static IEnumerable<TNumber> GroupedCumulativeMin<T, TKey, TNumber>(
        this IEnumerable<T> sequence,
        Func<T, TKey> groupKey,
        Func<T, TNumber> numericTarget,
        Dictionary<TKey, TNumber>? initialRecord = default
    ) where TNumber : INumber<TNumber> where TKey : notnull {
        var record = initialRecord ?? new Dictionary<TKey, TNumber>();

        foreach (var item in sequence) {
            var key = groupKey(item);
            var currentMin = numericTarget(item);

            if (record.TryGetValue(key, out var recordMin) && recordMin < currentMin) {
                yield return recordMin;
            }

            record[key] = currentMin;
            yield return currentMin;
        }
    }

    public static SortedDictionary<TKey, TElement> ToSortedDictionary<TSource, TKey, TElement>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TElement> elementSelector
    )
        where TKey : notnull {
        return new SortedDictionary<TKey, TElement>(source.ToDictionary(keySelector, elementSelector));
    }
}