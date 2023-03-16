namespace KL.Common.Extensions;


public static class CollectionExtensions {
    public static bool IsEmpty<T>(this ICollection<T> list) {
        return list.Count == 0;
    }
}