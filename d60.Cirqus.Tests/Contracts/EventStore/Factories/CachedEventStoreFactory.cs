using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Tests.MongoDb;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class CachedEventStoreFactory : IEventStoreFactory
    {
        readonly EventCache _eventStore;

        public CachedEventStoreFactory()
        {
            _eventStore = new EventCache(new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "events"));
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}