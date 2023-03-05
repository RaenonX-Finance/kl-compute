using KL.Common.Utils;

namespace KL.Common.Extensions;


public static class EnumExtensions {
    public static T GetPrev<T>(this T @enum) where T : Enum {
        if (!typeof(T).IsEnum) {
            throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
        }

        var enums = (T[])Enum.GetValues(@enum.GetType());
        // Not using `%` because it doesn't take care of negative ints
        return enums[NumberHelper.Mod(Array.IndexOf(enums, @enum) - 1, enums.Length)];
    }
}