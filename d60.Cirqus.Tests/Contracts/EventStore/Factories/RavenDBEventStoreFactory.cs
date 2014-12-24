using d60.Cirqus.Events;
using d60.Cirqus.RavenDB;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class RavenDBEventStoreFactory : IEventStoreFactory
    {
        public RavenDBEventStore _eventStore;

        public RavenDBEventStoreFactory()
        {
            _eventStore = new RavenDBEventStore(null, true);
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}