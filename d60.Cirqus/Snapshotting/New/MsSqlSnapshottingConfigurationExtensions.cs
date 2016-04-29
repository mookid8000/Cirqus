using System;

namespace d60.Cirqus.Snapshotting.New
{
    public static class MsSqlSnapshottingConfigurationExtensions
    {
        /// <summary>
        /// Configures Cirqus to use SQL Server to store aggregate root snapshots in the <paramref name="tableName"/> collection in the specified database.
        /// </summary>
        public static void UseSqlServer(this SnapshottingConfigurationBuilder builder, string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
        {
            if (builder == null) throw new ArgumentNullException("builder");
            if (tableName == null) throw new ArgumentNullException("tableName");

            builder.Register(c => new MsSqlSnapshotStore(connectionStringOrConnectionStringName, tableName, automaticallyCreateSchema));
        }

    }
}