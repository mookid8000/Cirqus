using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace d60.Cirqus.Snapshotting
{
    /// <summary>
    /// Sturdy object serializer that relies on JSON.NET's ability to pick up private fields. Should be able to reliably serialize
    /// everything that has a public default ctor.
    /// </summary>
    public class Sturdylizer
    {
        public string SerializeObject(object rootInstance)
        {
            return JsonConvert.SerializeObject(rootInstance, SerializerSettings);
        }

        public object DeserializeObject(string data)
        {
            return JsonConvert.DeserializeObject(data, SerializerSettings);
        }

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new JohnnyDeep(),
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented,
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        /// <summary>
        /// Deep-cloning contract resolver for JSON.NET
        /// </summary>
        class JohnnyDeep : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var inheritedProperties = new List<JsonProperty>();

                if (type.BaseType != null)
                {
                    // recursively add properties from base types
                    inheritedProperties.AddRange(CreateProperties(type.BaseType, memberSerialization));
                }

                var jsonPropertiesFromFields = type
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Select(f =>
                    {
                        var jsonProperty = base.CreateProperty(f, memberSerialization);
                        jsonProperty.Writable = jsonProperty.Readable = true;
                        return jsonProperty;
                    })
                    .ToArray();

                var jsonPropertiesToKeep = jsonPropertiesFromFields
                    .Concat(inheritedProperties)
                    .GroupBy(p => p.UnderlyingName)
                    .Select(g => g.First()) //< only keep first occurrency for each underlying name - weeds out dupes
                    .ToList();

                return jsonPropertiesToKeep;
            }
        }
 
    }
}