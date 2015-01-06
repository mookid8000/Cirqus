using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d60.Cirqus.Serialization
{
    public class GenericSerializer
    {
        readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        public string Serialize(object obj)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, _settings);
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not serialize {0}!", obj), exception);
            }
        }

        public object Deserialize(string json)
        {
            try
            {
                var deserializedObject = JsonConvert.DeserializeObject(json, _settings);

                if (deserializedObject is JObject)
                {
                    deserializedObject = DeserializeJObject((JObject)deserializedObject);
                }

                return deserializedObject;
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not deserialize {0}!", json), exception);
            }
        }

        object DeserializeJObject(JObject jObject)
        {
            const string propertyName = "$type";

            var typeProperty = jObject[propertyName];

            if (typeProperty == null)
            {
                throw new FormatException(string.Format("Could not find '$type' property when attempting to deserialize JSON object {0}", jObject));    
            }

            var typeName = typeProperty.ToString();
            var objectType = Type.GetType(typeName);

            if (objectType == null)
            {
                throw new FormatException(string.Format("Could not find .NET type {0} when attempting to deserialize JSON object {1}", typeName, jObject));
            }

            return jObject.ToObject(objectType);
        }
    }
}