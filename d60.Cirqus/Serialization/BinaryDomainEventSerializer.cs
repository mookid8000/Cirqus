using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using d60.Cirqus.Events;

namespace d60.Cirqus.Serialization
{
    /// <summary>
    /// Serializer that uses .NET's <see cref="BinaryFormatter"/> to do its thing
    /// </summary>
    public class BinaryDomainEventSerializer : IDomainEventSerializer
    {
        public EventData Serialize(DomainEvent e)
        {
            using (var result = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(result, e);
                return EventData.FromDomainEvent(e, result.GetBuffer());
            }
        }

        public DomainEvent Deserialize(EventData e)
        {
            using (var data = new MemoryStream(e.Data))
            {
                var formatter = new BinaryFormatter();
                var domainEvent = (DomainEvent) formatter.Deserialize(data);
                domainEvent.Meta = e.Meta;
                return domainEvent;
            }
        }
    }
}