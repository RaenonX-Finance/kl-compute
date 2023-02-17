using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace KL.Common.Controllers.MongoSerializers;


internal sealed class TimeOnlyBsonSerializer : StructSerializerBase<TimeOnly> {
    private readonly IBsonSerializer<TimeSpan> _innerSerializer = new TimeSpanSerializer();

    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
        return TimeOnly.FromTimeSpan(_innerSerializer.Deserialize(context, args));
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value) {
        _innerSerializer.Serialize(context, args, value.ToTimeSpan());
    }
}