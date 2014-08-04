using d60.EventSorcerer.Events;
using d60.EventSorcerer.TestHelpers;
using d60.EventSorcerer.TestHelpers.Internals;

namespace d60.EventSorcerer.Tests.Contracts.EventStore.Factories
{
    public class InMemoryEventStoreFactory : IEventStoreFactory
    {
        readonly InMemoryEventStore _eventStore;

        public InMemoryEventStoreFactory()
        {
            _eventStore = new InMemoryEventStore();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}