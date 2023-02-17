using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace KL.Common.Controllers.MongoSerializers;


internal sealed class TimeZoneInfoBsonSerializer : SerializerBase<TimeZoneInfo> {
    private readonly IBsonSerializer<string> _innerSerializer = new StringSerializer();

    public override TimeZoneInfo Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) {
        return TimeZoneInfo.FindSystemTimeZoneById(_innerSerializer.Deserialize(context, args));
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeZoneInfo value) {
        _innerSerializer.Serialize(context, args, value.Id);
    }
}