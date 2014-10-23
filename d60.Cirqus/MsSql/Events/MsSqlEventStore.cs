using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.MsSql.Events
{
    public class MsSqlEventStore : IEventStore
    {
        readonly string _tableName;
        readonly Func<SqlConnection> _connectionProvider;
        readonly Action<SqlConnection> _cleanupAction;
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer("<events>");

        public MsSqlEventStore(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
        {
            _tableName = tableName;

            var connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);

            _connectionProvider = () =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            };

            _cleanupAction = connection => connection.Dispose();

            if (automaticallyCreateSchema)
            {
                CreateSchema();
            }
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            eventList.ForEach(e => _domainEventSerializer.EnsureSerializability(e));

            try
            {
                WithConnection(conn =>
                {
                    using (var tx = conn.BeginTransaction())
                    {
                        var globalSequenceNumber = GetNextGlobalSequenceNumber(conn, tx);

                        foreach (var e in eventList)
                        {
                            e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = globalSequenceNumber++;
                            e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;
                        }

                        EventValidation.ValidateBatchIntegrity(batchId, eventList);

                        foreach (var e in eventList)
                        {
                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                cmd.CommandText = string.Format(@"

INSERT INTO [{0}] (
    [batchId],
    [aggId],
    [seqNo],
    [globSeqNo],
    [data]
) VALUES (
    @batchId,
    @aggId,
    @seqNo,
    @globSeqNo,
    @data
)

", _tableName);
                                cmd.Parameters.Add("batchId", SqlDbType.UniqueIdentifier).Value = batchId;
                                cmd.Parameters.Add("aggId", SqlDbType.UniqueIdentifier).Value = new Guid(e.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString());
                                cmd.Parameters.Add("seqNo", SqlDbType.BigInt).Value = e.Meta[DomainEvent.MetadataKeys.SequenceNumber];
                                cmd.Parameters.Add("globSeqNo", SqlDbType.BigInt).Value = e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber];
                                cmd.Parameters.Add("data", SqlDbType.NVarChar).Value = _domainEventSerializer.Serialize(e);

                                cmd.ExecuteNonQuery();
                            }
                        }

                        tx.Commit();
                    }
                });
            }
            catch (SqlException sqlException)
            {
                if (sqlException.Errors.Cast<SqlError>().Any(e => e.Number == 2601))
                {
                    throw new ConcurrencyException(batchId, eventList, sqlException);
                }

                throw;
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            var globalSequenceNumber = 0L;

            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    globalSequenceNumber = GetNextGlobalSequenceNumber(conn, tx);
                }
            });

            return globalSequenceNumber;
        }

        long GetNextGlobalSequenceNumber(SqlConnection conn, SqlTransaction tx)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format("SELECT MAX(globSeqNo) FROM [{0}]", _tableName);

                var result = cmd.ExecuteScalar();

                return result != DBNull.Value
                    ? (long)result + 1
                    : 0;
            }
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            SqlConnection conn = null;

            try
            {
                conn = _connectionProvider();

                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"

SELECT [data] FROM [{0}] WHERE [aggId] = @aggId AND [seqNo] >= @firstSeqNo

", _tableName);
                        cmd.Parameters.Add("aggId", SqlDbType.UniqueIdentifier).Value = aggregateRootId;
                        cmd.Parameters.Add("firstSeqNo", SqlDbType.BigInt).Value = firstSeq;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = (string) reader["data"];

                                yield return _domainEventSerializer.Deserialize(data);
                            }
                        }
                    }

                    tx.Commit();
                }
            }
            finally
            {
                if (conn != null)
                {
                    _cleanupAction(conn);
                }
            }
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            SqlConnection connection = null;

            try
            {
                connection = _connectionProvider();

                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"

SELECT [data] FROM [{0}] WHERE [globSeqNo] >= @cutoff ORDER BY [globSeqNo]

", _tableName);

                        cmd.Parameters.Add("cutoff", SqlDbType.BigInt).Value = globalSequenceNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = (string)reader["data"];

                                yield return _domainEventSerializer.Deserialize(data);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (connection != null)
                {
                    _cleanupAction(connection);
                }
            }
        }

        /// <summary>
        /// WARNING: WILL DROP ALL EVENTS WITHOUT WARNING
        /// </summary>
        public void DropEvents()
        {
            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    DELETE FROM [{0}]
END
", _tableName);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            });
        }

        void CreateSchema()
        {
            WithConnection(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    CREATE TABLE [dbo].[{0}] (
	    [id] [bigint] IDENTITY(1,1) NOT NULL,
	    [batchId] [uniqueidentifier] NOT NULL,
	    [aggId] [uniqueidentifier] NOT NULL,
	    [seqNo] [bigint] NOT NULL,
	    [globSeqNo] [bigint] NOT NULL,
	    [data] [nvarchar](max) NOT NULL,

        CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
        (
	        [id] ASC
        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )

    CREATE UNIQUE NONCLUSTERED INDEX [IDX_{0}_aggIdSeqNo] ON [dbo].[{0}]
    (
	    [aggId] ASC,
	    [seqNo] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)

    CREATE UNIQUE NONCLUSTERED INDEX [IDX_{0}_globSeq] ON [dbo].[{0}]
    (
	    [globSeqNo] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
END

", _tableName);

                    cmd.ExecuteNonQuery();
                }
            });
        }

        void WithConnection(Action<SqlConnection> action)
        {
            SqlConnection connection = null;

            try
            {
                connection = _connectionProvider();

                action(connection);
            }
            finally
            {
                if (connection != null)
                {
                    _cleanupAction(connection);
                }
            }
        }
    }
}
