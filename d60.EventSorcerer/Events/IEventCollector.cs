namespace d60.EventSorcerer.Events
{
    public interface IEventCollector
    {
        void Add(DomainEvent e);
    }
}