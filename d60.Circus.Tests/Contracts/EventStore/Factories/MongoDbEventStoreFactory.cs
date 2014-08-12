using d60.Circus.Events;
using d60.Circus.MongoDb.Events;
using d60.Circus.Tests.MongoDb;

namespace d60.Circus.Tests.Contracts.EventStore.Factories
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