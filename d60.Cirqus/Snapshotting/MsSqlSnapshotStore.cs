using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.MsSql;
using d60.Cirqus.Snapshotting.New;

namespace d60.Cirqus.Snapshotting
{
    class MsSqlSnapshotStore : ISnapshotStore
    {
        readonly Sturdylizer _sturdylizer = new Sturdylizer();
        readonly string _tableName;
        readonly string _connectionString;

        public MsSqlSnapshotStore(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
        {
            _tableName = tableName;
            _connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);

            if (automaticallyCreateSchema)
            {
                CreateSchema();
            }
        }

        void CreateSchema()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = string.Format(@"

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    CREATE TABLE [dbo].[{0}] (
	    [id] [nvarchar](200) NOT NULL,
	    [aggId] [nvarchar](255) NOT NULL,
	    [data] [nvarchar](MAX) NOT NULL,
	    [validFromglobSeqNo] [bigint] NOT NULL,
	    [version] [int] NOT NULL,
	    [lastUsedUtc] [datetime] NOT NULL,

        CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED (
            [id] ASC
        )
    )

    CREATE UNIQUE NONCLUSTERED INDEX [IDX_{0}_aggIdSeqNo] ON [dbo].[{0}]
    (
	    [aggId] ASC,
	    [version] ASC,
	    [validFromglobSeqNo] ASC
    )
END

", _tableName);

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        public Snapshot LoadSnapshot<TAggregateRoot>(string aggregateRootId, long maxGlobalSequenceNumber)
        {
            var snapshotAttribute = GetSnapshotAttribute<TAggregateRoot>();

            if (snapshotAttribute == null)
            {
                return null;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        $@"

SELECT TOP 1 * FROM [{_tableName}] 

WHERE [aggId] = @aggId 
    AND [version] = @version 
    AND [validFromglobSeqNo] < @globalSequenceNumber

";

                    command.Parameters.Add("aggId", SqlDbType.NVarChar, 255).Value = aggregateRootId;
                    command.Parameters.Add("version", SqlDbType.Int).Value = snapshotAttribute.Version;
                    command.Parameters.Add("globalSequenceNumber", SqlDbType.BigInt).Value = maxGlobalSequenceNumber;

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                var data = (string)reader["data"];
                                var instance = _sturdylizer.DeserializeObject(data);
                                var validFromGlobalSequenceNumber = (long) reader["validFromglobSeqNo"];
                                return new Snapshot(validFromGlobalSequenceNumber, instance);
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine();
                                return null;
                            }
                        }
                        return null;
                    }
                }
            }
        }

        public void SaveSnapshot<TAggregateRoot>(string aggregateRootId, AggregateRoot aggregateRootInstance, long validFromGlobalSequenceNumber)
        {
            var snapshotAttribute = GetSnapshotAttribute(aggregateRootInstance.GetType());
            var info = new AggregateRootInfo(aggregateRootInstance);
            var serializedInstance = _sturdylizer.SerializeObject(info.Instance);

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                /*
	    [id] [nvarchar](200) NOT NULL,
	    [aggId] [nvarchar](255) NOT NULL,
	    [data] [nvarchar](MAX) NOT NULL,
	    [validFromglobSeqNo] [bigint] NOT NULL,
	    [version] [int] NOT NULL,
	    [lastUsedUtc] [datetime] NOT NULL,
                 * */
                try
                {
                    using (var commmand = connection.CreateCommand())
                    {
                        commmand.CommandText = $@"
INSERT INTO [{_tableName}] (
    [id], 
    [aggId],
    [data],
    [validFromGlobSeqNo],
    [version],
    [lastUsedUtc]
) VALUES (
    @id,
    @aggId,
    @data,
    @validFromGlobalSequenceNumber,
    @version,
    @lastUsedUtc
)
";
                        commmand.Parameters.Add("id", SqlDbType.NVarChar, 200).Value = $"{aggregateRootId}/{info.SequenceNumber}";
                        commmand.Parameters.Add("aggId", SqlDbType.NVarChar, 255).Value = aggregateRootId;
                        commmand.Parameters.Add("data", SqlDbType.NVarChar).Value = serializedInstance;
                        commmand.Parameters.Add("validFromGlobalSequenceNumber", SqlDbType.BigInt).Value = validFromGlobalSequenceNumber;
                        commmand.Parameters.Add("version", SqlDbType.Int).Value = snapshotAttribute.Version;
                        commmand.Parameters.Add("lastUsedUtc", SqlDbType.DateTime).Value = DateTime.UtcNow;

                        commmand.ExecuteNonQuery();
                    }
                }
                catch (SqlException)
                {
                }
            }
        }

        static EnableSnapshotsAttribute GetSnapshotAttribute<TAggregateRoot>()
        {
            return GetSnapshotAttribute(typeof(TAggregateRoot));
        }

        static EnableSnapshotsAttribute GetSnapshotAttribute(Type type)
        {
            return type
                .GetCustomAttributes(typeof(EnableSnapshotsAttribute), false)
                .Cast<EnableSnapshotsAttribute>()
                .FirstOrDefault();
        }
    }
}