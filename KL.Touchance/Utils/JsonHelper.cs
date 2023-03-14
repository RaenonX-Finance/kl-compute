using System.Text.Json;
using System.Text.Json.Serialization;
using KL.Touchance.Subscriptions;
using Serilog;

namespace KL.Touchance.Utils;


public class JsonAllCapsNamingPolicy : JsonNamingPolicy {
    public override string ConvertName(string name) {
        return name.ToUpper();
    }
}

public static class JsonHelper {
    private static readonly ILogger Log = Serilog.Log.ForContext(typeof(JsonHelper));

    public static readonly JsonSerializerOptions SerializingOptions = new() {
        Converters = { new JsonStringEnumConverter(new JsonAllCapsNamingPolicy()) }
    };

    public static T Deserialize<T>(this string message) {
        try {
            var deserialized = JsonSerializer.Deserialize<T>(message.Trim('\0'), SerializingOptions);

            if (deserialized is null) {
                throw new JsonException("`JsonSerializer.Deserialize()` returns `null`");
            }

            return deserialized;
        } catch (JsonException) {
            Log.Error("Failed to deserialize message: {Message}", message);
            throw;
        }
    }

    public static TcSubscription ToTcSubscription(this string message) {
        return message.Deserialize<TcSubscription>();
    }
}