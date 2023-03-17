using System.Text.Json;
using JetBrains.Annotations;
using KL.Touchance.Utils;

namespace KL.Touchance.Requests;


public abstract record TcRequest {
    [UsedImplicitly]
    public abstract string Request { get; }

    public string ToJson() {
        // `GetType()` returns derived class for correct JSON serialization
        return JsonSerializer.Serialize(this, GetType(), JsonHelper.SerializingOptions);
    }
}