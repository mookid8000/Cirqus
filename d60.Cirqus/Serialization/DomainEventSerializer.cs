using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d60.Cirqus.Serialization
{
    public class DomainEventSerializer : IDomainEventSerializer
    {
        readonly TypeAliasBinder _binder;

        public DomainEventSerializer()
            : this("<events>")
        {
        }

        public DomainEventSerializer(string virtualNamespaceName)
        {
            _binder = new TypeAliasBinder(virtualNamespaceName);
            Settings = new JsonSerializerSettings
            {
                Binder = _binder.AddType(typeof(Metadata)),
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented
            };
        }

        public JsonSerializerSettings Settings { get; private set; }

        public DomainEventSerializer AddAliasFor(Type type)
        {
            _binder.AddType(type);
            return this;
        }

        public DomainEventSerializer AddAliasesFor(params Type[] types)
        {
            return AddAliasesFor((IEnumerable<Type>)types);
        }

        public DomainEventSerializer AddAliasesFor(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AddAliasFor(type);
            }
            return this;
        }

        public string Serialize(DomainEvent e)
        {
            try
            {
                return JsonConvert.SerializeObject(e, Settings);
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not serialize DomainEvent {0} into JSON!", e), exception);
            }
        }

        public DomainEvent Deserialize(string text)
        {
            try
            {
                var deserializedObject = JsonConvert.DeserializeObject(text, Settings);

                // if the $type property is not first in the JSON (e.g. after having been roundtripped to Postgres), it will come back as a JObject - therefore:
                if (deserializedObject is JObject)
                {
                    var jsonObject = (JObject)deserializedObject;
                    var eventTypeName = jsonObject["$type"].ToString();
                    var eventType = Type.GetType(eventTypeName);

                    if (eventType == null)
                    {
                        throw new FormatException(string.Format("Could not find .NET type {0}", eventTypeName));
                    }

                    var bim = jsonObject.ToObject(eventType);

                    return (DomainEvent)bim;
                }

                if (deserializedObject is DomainEvent)
                {
                    return (DomainEvent)deserializedObject;
                }

                throw new ApplicationException("Deserialized object was not JObject or DomainEvent!");
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not deserialize JSON text '{0}' into proper DomainEvent!", text), exception);
            }
        }

        public void EnsureSerializability(DomainEvent domainEvent)
        {
            var firstSerialization = Serialize(domainEvent);

            var secondSerialization = Serialize(Deserialize(firstSerialization));

            if (firstSerialization.Equals(secondSerialization)) return;

            throw new ArgumentException(string.Format(@"Could not properly roundtrip the following domain event: {0}

Result after first serialization:

{1}

Result after roundtripping:

{2}", domainEvent, firstSerialization, secondSerialization));
        }

        public Event DoSerialize(DomainEvent e)
        {
            var text = Serialize(e);
            var data = Encoding.UTF8.GetBytes(text);

            var result = new Event
            {
                Meta = e.Meta,
                Data = data
            };

            result.MarkAsJson();

            return result;
        }

        public DomainEvent DoDeserialize(Event e)
        {
            var meta = e.Meta;
            var text = Encoding.UTF8.GetString(e.Data);

            var domainEvent = Deserialize(text);

            return domainEvent;
        }
    }
}