using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Tests.MongoDb;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class MongoDbEventStoreFactory : IEventStoreFactory
    {
        readonly MongoDbEventStore _eventStore;

        public MongoDbEventStoreFactory()
        {
            _eventStore = new MongoDbEventStore(MongoHelper.InitializeTestDatabase(), "events");
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}