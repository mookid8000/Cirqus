using d60.Cirqus.Events;

namespace d60.Cirqus.Tests.Contracts.EventStore
{
    public interface IEventStoreFactory
    {
        IEventStore GetEventStore();
    }
}