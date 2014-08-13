using System.Collections.Generic;
using d60.Cirqus.Events;
using Newtonsoft.Json;

namespace d60.Cirqus.AzureServiceBus
{
    public class Serializer
    {
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public string Serialize(List<DomainEvent> domainEvents)
        {
            return JsonConvert.SerializeObject(domainEvents, Settings);
        }

        public List<DomainEvent> Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<List<DomainEvent>>(json, Settings);
        }
    }
}