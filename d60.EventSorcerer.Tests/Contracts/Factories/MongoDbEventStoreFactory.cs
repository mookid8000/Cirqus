using d60.EventSorcerer.Events;
using d60.EventSorcerer.MongoDb.Events;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Tests.MongoDb;

namespace d60.EventSorcerer.Tests.Contracts.Factories
{
    public class MongoDbEventStoreFactory : IEventStoreFactory
    {
        readonly MongoDbEventStore _eventStore;

        public MongoDbEventStoreFactory()
        {
            _eventStore = new MongoDbEventStore(Helper.InitializeTestDatabase(), "events");
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }

        public ISequenceNumberGenerator GetSequenceNumberGenerator()
        {
            return _eventStore;
        }
    }
}