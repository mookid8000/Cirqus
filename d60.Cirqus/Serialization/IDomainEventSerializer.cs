using d60.Cirqus.Events;

namespace d60.Cirqus.Serialization
{
    public interface IDomainEventSerializer
    {
        EventData Serialize(DomainEvent e);
        DomainEvent Deserialize(EventData e);
    }
}