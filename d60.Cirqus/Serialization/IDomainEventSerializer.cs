using d60.Cirqus.Events;

namespace d60.Cirqus.Serialization
{
    /// <summary>
    /// Serializer that is capable of serializing/deserializing domain events
    /// </summary>
    public interface IDomainEventSerializer
    {
        /// <summary>
        /// Serializes the given domain event into an <see cref="EventData"/> object
        /// </summary>
        EventData Serialize(DomainEvent e);
        
        /// <summary>
        /// Deserialized the given <see cref="EventData"/> into the right domain event
        /// </summary>
        DomainEvent Deserialize(EventData e);
    }
}