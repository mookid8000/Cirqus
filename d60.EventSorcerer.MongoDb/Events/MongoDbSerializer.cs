using System.Text.RegularExpressions;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace d60.EventSorcerer.MongoDb.Events
{
    public class MongoDbSerializer
    {
        static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            Formatting = Formatting.Indented,
            Binder = new TypeAliasBinder("<events>")
                .AddType(typeof(Metadata))
        };

        const string JsonDotNetTypeProperty = @"""$type"":";
        public const string JsonDotNetTypePropertyRegex = @"""\$type""[ \t]*\:";

        const string BsonTypeProperty = @"""_t"":";
        public const string BsonTypePropertyRegex = @"""_t""[ \t]*\:";

        public BsonDocument Serialize(DomainEvent e)
        {
            var jsonText = JsonConvert.SerializeObject(e, JsonSerializerSettings);
            jsonText = Regex.Replace(jsonText, JsonDotNetTypePropertyRegex, BsonTypeProperty);
            var doc = BsonDocument.Parse(jsonText);
            return doc;
        }

        public DomainEvent Deserialize(BsonValue o)
        {
            var jsonText = o.ToString();
            jsonText = Regex.Replace(jsonText, BsonTypePropertyRegex, JsonDotNetTypeProperty);
            return (DomainEvent) JsonConvert.DeserializeObject(jsonText, JsonSerializerSettings);
        }
    }
}