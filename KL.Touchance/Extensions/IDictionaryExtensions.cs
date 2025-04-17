using System.Text.Json;
using KL.Touchance.Utils;

namespace KL.Touchance.Extensions;


public static class DictionaryExtensions {
    public static T ToModel<T>(this IDictionary<string, object> data) {
        var model = JsonSerializer.Deserialize<T>(
            JsonSerializer.Serialize(data, JsonHelper.SerializingOptions),
            JsonHelper.SerializingOptions
        );

        if (model is null) {
            throw new InvalidDataException($"`data` given cannot be serialized into type of {typeof(T)}");
        }

        return model;
    }
}