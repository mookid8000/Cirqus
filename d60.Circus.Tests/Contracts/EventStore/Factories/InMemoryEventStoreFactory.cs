using d60.Circus.Events;
using d60.Circus.TestHelpers.Internals;

namespace d60.Circus.Tests.Contracts.EventStore.Factories
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