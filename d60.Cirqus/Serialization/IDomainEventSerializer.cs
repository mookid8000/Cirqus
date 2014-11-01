using d60.Cirqus.Events;

namespace d60.Cirqus.Serialization
{
    public interface IDomainEventSerializer
    {
        Event Serialize(DomainEvent e);
        DomainEvent Deserialize(Event e);
    }
}