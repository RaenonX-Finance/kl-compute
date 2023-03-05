using KL.Common.Controllers.MongoSerializers;
using KL.Common.Enums;
using KL.Common.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace KL.Common.Extensions;


public static class MongoExtensions {
    public static string GetSessionId(this IClientSessionHandle session) {
        return session.WrappedCoreSession.Id["id"].AsGuid.ToString();
    }

    public static IMongoClient Initialize(this IMongoClient client) {
        RegisterConvention();
        RegisterSerializer();

        return client;
    }

    private static void RegisterConvention() {
        ConventionRegistry.Register(
            name: "CamelCaseConvention",
            conventions: new ConventionPack { new CamelCaseElementNameConvention() },
            filter: _ => true
        );
    }

    private static void RegisterSerializer() {
        RegisterGlobalSerializer();
        RegisterFieldSpecificSerializer();
    }

    private static void RegisterGlobalSerializer() {
        BsonSerializer.RegisterSerializer(new EnumSerializer<PxSource>(BsonType.String));
        BsonSerializer.RegisterSerializer(new EnumSerializer<ProductCategory>(BsonType.String));
        BsonSerializer.RegisterSerializer(new EnumSerializer<HistoryInterval>(BsonType.String));
        BsonSerializer.RegisterSerializer(new EnumSerializer<DayOfWeek>(BsonType.String));
        BsonSerializer.RegisterSerializer(new EnumSerializer<SrLevelType>(BsonType.String));
        // By default, `decimal` are stored in `string`, which is undesired
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
        // `TimeOnly.ticks` is serialized, but no corresponding constructor is taking `ticks` 
        BsonSerializer.RegisterSerializer(new TimeOnlyBsonSerializer());
        // `TimezoneInfo` is serialized, but deserializer doesn't take `_id`
        BsonSerializer.RegisterSerializer(new TimeZoneInfoBsonSerializer());
    }

    private static void RegisterFieldSpecificSerializer() {
        // `CalculatedDataModel.Ema` has numeric key (EMA period)
        BsonClassMap.RegisterClassMap<CalculatedDataModel>(
            cm => {
                cm.AutoMap();
                cm.GetMemberMap(c => c.Ema)
                    .SetSerializer(
                        new DictionaryInterfaceImplementerSerializer<Dictionary<int, double?>>(
                            DictionaryRepresentation.Document,
                            new Int32Serializer(BsonType.String),
                            BsonSerializer.SerializerRegistry.GetSerializer<double?>()
                        )
                    );
            }
        );
    }
}