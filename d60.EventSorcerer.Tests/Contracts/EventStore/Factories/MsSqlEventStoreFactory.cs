using d60.EventSorcerer.Events;
using d60.EventSorcerer.MsSql;
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

            var connectionString = SqlHelper.GetConnectionString(TestSqlHelper.ConnectionStringName);
            
            TestSqlHelper.DropTable(connectionString, "events");

            _eventStore = new MsSqlEventStore(TestSqlHelper.ConnectionStringName, "events");
            
            _eventStore.DropEvents();
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}