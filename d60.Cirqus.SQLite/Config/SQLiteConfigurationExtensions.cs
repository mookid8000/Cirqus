using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;

namespace d60.Cirqus.SQLite.Config
{
    public static class SQLiteConfigurationExtensions
    {
        /// <summary>
        /// Configures Cirqus to use SQLite as the event store
        /// </summary>
        public static void UseSQLite(this EventStoreConfigurationBuilder builder, string databasePath)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (databasePath == null) throw new ArgumentNullException("databasePath");

            builder.Registrar.Register<IEventStore>(context => new SQLiteEventStore(databasePath));
        }
    }
}