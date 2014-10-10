using System;
using System.IO;
using d60.Cirqus.Events;
using d60.Cirqus.SQLite;

namespace d60.Cirqus.Tests.Contracts.EventStore.Factories
{
    public class SQLiteEventStoreFactory : IEventStoreFactory
    {
        readonly string _databasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "events.db");
        readonly SQLiteEventStore _eventStore;

        public SQLiteEventStoreFactory()
        {
            var directoryName = Path.GetDirectoryName(_databasePath);

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
            
            _eventStore = new SQLiteEventStore(_databasePath);
        }

        public IEventStore GetEventStore()
        {
            return _eventStore;
        }
    }
}