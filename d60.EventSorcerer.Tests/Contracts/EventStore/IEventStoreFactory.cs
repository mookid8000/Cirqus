using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Tests.Contracts.EventStore
{
    public interface IEventStoreFactory
    {
        IEventStore GetEventStore();
    }
}