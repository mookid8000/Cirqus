using System;
using System.Data.SqlClient;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.MsSql;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Tests.Contracts.EventStore.Factories
{
    public class MsSqlEventStoreFactory : IEventStoreFactory
    {
        const string ConnectionStringName = "testdb";
        readonly MsSqlEventStore _eventStore;

        public MsSqlEventStoreFactory()
        {
            EnsureTestDatabaseExists();

            _eventStore = new MsSqlEventStore(ConnectionStringName, "events");
            
            _eventStore.DropEvents();
        }

        void EnsureTestDatabaseExists()
        {
            var connectionString = SqlHelper.GetConnectionString(ConnectionStringName);
            var databaseName = SqlHelper.GetDatabaseName(connectionString);
            var masterConnectionString = connectionString.Replace(databaseName, "master");

            try
            {
                using (var conn = new SqlConnection(masterConnectionString))
                {
                    conn.Open();

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = string.Format(@"
BEGIN
    CREATE DATABASE [{0}]
END

", databaseName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("An exception occurred while trying to create test database '{0}': {1} - this is not necessarily an error, because everything should work fine if the test database already exists", 
                    databaseName, exception);
            }
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