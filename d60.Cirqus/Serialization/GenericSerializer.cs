using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

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
                return JsonConvert.DeserializeObject(json, _settings);
            }
            catch (Exception exception)
            {
                throw new SerializationException(string.Format("Could not deserialize {0}!", json), exception);
            }
        }
    }
}