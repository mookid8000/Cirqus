using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace d60.Cirqus.Serialization
{
    public class JsonDomainEventSerializer : IDomainEventSerializer
    {
        readonly TypeAliasBinder _binder;
        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public JsonDomainEventSerializer()
            : this("<events>")
        {
        }

        public JsonDomainEventSerializer(string virtualNamespaceName)
        {
            _binder = new TypeAliasBinder(virtualNamespaceName);

            Settings = new JsonSerializerSettings
            {
                ContractResolver = new ContractResolver(),
                Binder = _binder.AddType(typeof(Metadata)),
                TypeNameHandling = TypeNameHandling.Objects,
                Formatting = Formatting.Indented,
            };
        }

        public class ContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var jsonProperties = base.CreateProperties(type, memberSerialization)
                    .Where(property => property.DeclaringType != typeof (DomainEvent) && property.PropertyName != "Meta")
                    .ToList();

                return jsonProperties;
            }
        }

        public JsonSerializerSettings Settings { get; private set; }

        public JsonDomainEventSerializer AddAliasFor(Type type)
        {
            _binder.AddType(type);
            return this;
        }

        public JsonDomainEventSerializer AddAliasesFor(params Type[] types)
        {
            return AddAliasesFor((IEnumerable<Type>)types);
        }

        public JsonDomainEventSerializer AddAliasesFor(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                AddAliasFor(type);
            }
            return this;
        }

        public Event Serialize(DomainEvent e)
        {
            try
            {
                var jsonText = JsonConvert.SerializeObject(e, Settings);
                var data = DefaultEncoding.GetBytes(jsonText);

                var result = new Event
                {
                    Meta = e.Meta.Clone(),
                    Data = data
                };

                result.MarkAsJson();

                return result;
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not serialize DomainEvent {0} into JSON!", e), exception);
            }
        }

        public DomainEvent Deserialize(Event e)
        {
            var meta = e.Meta.Clone();
            var text = DefaultEncoding.GetString(e.Data);

            try
            {
                var deserializedObject = (DomainEvent)JsonConvert.DeserializeObject(text, Settings);
                deserializedObject.Meta = meta;
                return deserializedObject;
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

    }
}