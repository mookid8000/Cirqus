using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class InMemoryEventStoreFactory : IEventStoreFactory
    {
        readonly InMemoryEventStore _eventStore;

        public InMemoryEventStoreFactory()
        {
            _eventStore = new InMemoryEventStore(new JsonDomainEventSerializer());
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}