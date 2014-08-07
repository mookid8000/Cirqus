using d60.EventSorcerer.Events;
using d60.EventSorcerer.MsSql.Events;
using d60.EventSorcerer.Tests.MsSql;

namespace d60.EventSorcerer.Tests.Contracts.EventStore.Factories
{
    public class MsSqlEventStoreFactory : IEventStoreFactory
    {
        readonly MsSqlEventStore _eventStore;

        public MsSqlEventStoreFactory()
        {
            TestSqlHelper.EnsureTestDatabaseExists();

            var connectionString = TestSqlHelper.ConnectionString;
            
            TestSqlHelper.DropTable("events");

            _eventStore = new MsSqlEventStore(connectionString, "events");
            
            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}