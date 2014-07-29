namespace d60.EventSorcerer.Events
{
    /// <summary>
    /// A thing that is capable of collecting emitted events
    /// </summary>
    public interface IEventCollector
    {
        void Add(DomainEvent e);
    }
}