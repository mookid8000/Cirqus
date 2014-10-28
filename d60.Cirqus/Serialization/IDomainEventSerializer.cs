using d60.Cirqus.Events;

namespace d60.Cirqus.Serialization
{
    public interface IDomainEventSerializer
    {
        Event DoSerialize(DomainEvent e);
        DomainEvent DoDeserialize(Event e);
    }
}