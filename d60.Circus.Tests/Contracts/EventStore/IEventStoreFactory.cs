using d60.Circus.Events;

namespace d60.Circus.Tests.Contracts.EventStore
{
    public interface IEventStoreFactory
    {
        IEventStore GetEventStore();
    }
}