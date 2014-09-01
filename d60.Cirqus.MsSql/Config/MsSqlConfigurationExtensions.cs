using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.MsSql.Events;

namespace d60.Cirqus.MsSql.Config
{
    public static class MsSqlConfigurationExtensions
    {
        public static void StoreInSqlServer(this EventStoreConfigurationBuilder builder, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (connectionStringOrConnectionStringName == null) throw new ArgumentNullException("connectionStringOrConnectionStringName");
            if (tableName == null) throw new ArgumentNullException("tableName");
            
            builder.Registrar.Register<IEventStore>(() => new MsSqlEventStore(connectionStringOrConnectionStringName, tableName, automaticallyCreateSchema: automaticallyCreateSchema));
        }
    }
}