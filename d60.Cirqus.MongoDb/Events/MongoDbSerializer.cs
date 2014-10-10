using System;
using System.Text.RegularExpressions;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace d60.Cirqus.MongoDb.Events
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
            var jsonText = SerializeToString(e);
            var doc = BsonDocument.Parse(jsonText);
            return doc;
        }

        public DomainEvent Deserialize(BsonValue o)
        {
            var jsonText = o.ToString();
            try
            {
                return DeserializeFromString(jsonText);
            }
            catch (Exception exception)
            {
                throw new FormatException(string.Format("Could not properly deserialize '{0}' into DomainEvent", jsonText), exception);
            }
        }

        public void EnsureSerializability(DomainEvent domainEvent)
        {
            var firstSerialization = SerializeToString(domainEvent);

            var secondSerialization = SerializeToString(DeserializeFromString(firstSerialization));

            if (firstSerialization.Equals(secondSerialization)) return;

            throw new ArgumentException(string.Format(@"Could not properly roundtrip the following domain event: {0}

Result after first serialization:

{1}

Result after roundtripping:

{2}", domainEvent, firstSerialization, secondSerialization));
        }

        static DomainEvent DeserializeFromString(string jsonText)
        {
            jsonText = Regex.Replace(jsonText, BsonTypePropertyRegex, JsonDotNetTypeProperty);
            return (DomainEvent)JsonConvert.DeserializeObject(jsonText, JsonSerializerSettings);
        }

        static string SerializeToString(DomainEvent e)
        {
            var jsonText = JsonConvert.SerializeObject(e, JsonSerializerSettings);
            jsonText = Regex.Replace(jsonText, JsonDotNetTypePropertyRegex, BsonTypeProperty);
            return jsonText;
        }
    }
}