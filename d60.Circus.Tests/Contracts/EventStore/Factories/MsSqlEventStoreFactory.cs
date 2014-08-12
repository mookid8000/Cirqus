using d60.Circus.Events;
using d60.Circus.MsSql.Events;
using d60.Circus.Tests.MsSql;

namespace d60.Circus.Tests.Contracts.EventStore.Factories
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