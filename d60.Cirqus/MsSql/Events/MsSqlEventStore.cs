using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Numbers;
using Newtonsoft.Json;

namespace d60.Cirqus.MsSql.Events
{
    public class MsSqlEventStore : IEventStore
    {
        readonly string _tableName;
        readonly Func<SqlConnection> _connectionProvider;
        readonly Action<SqlConnection> _cleanupAction;

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

        public void Save(Guid batchId, IEnumerable<Event> events)
        {
            var eventList = events.ToList();

            try
            {
                WithConnection(conn =>
                {
                    using (var tx = conn.BeginTransaction())
                    {
                        var globalSequenceNumber = GetNextGlobalSequenceNumber(conn, tx);

                        foreach (var e in eventList)
                        {
                            e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (globalSequenceNumber++).ToString(Metadata.NumberCulture);
                            e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
                        }

                        EventValidation.ValidateBatchIntegrity(batchId, eventList);

                        foreach (var @event in eventList)
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
    [meta],
    [data]
) VALUES (
    @batchId,
    @aggId,
    @seqNo,
    @globSeqNo,
    @meta,
    @data
)

", _tableName);
                                cmd.Parameters.Add("batchId", SqlDbType.UniqueIdentifier).Value = batchId;
                                cmd.Parameters.Add("aggId", SqlDbType.UniqueIdentifier).Value = new Guid(@event.Meta[DomainEvent.MetadataKeys.AggregateRootId].ToString());
                                cmd.Parameters.Add("seqNo", SqlDbType.BigInt).Value = @event.Meta[DomainEvent.MetadataKeys.SequenceNumber];
                                cmd.Parameters.Add("globSeqNo", SqlDbType.BigInt).Value = @event.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber];
                                cmd.Parameters.Add("meta", SqlDbType.NVarChar).Value = JsonConvert.SerializeObject(@event.Meta);
                                cmd.Parameters.Add("data", SqlDbType.VarBinary).Value = @event.Data;

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

        public IEnumerable<Event> Load(Guid aggregateRootId, long firstSeq = 0)
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

SELECT [meta],[data] FROM [{0}] WHERE [aggId] = @aggId AND [seqNo] >= @firstSeqNo

", _tableName);
                        cmd.Parameters.Add("aggId", SqlDbType.UniqueIdentifier).Value = aggregateRootId;
                        cmd.Parameters.Add("firstSeqNo", SqlDbType.BigInt).Value = firstSeq;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                yield return ReadEvent(reader);
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

        public IEnumerable<Event> Stream(long globalSequenceNumber = 0)
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

SELECT [meta],[data] FROM [{0}] WHERE [globSeqNo] >= @cutoff ORDER BY [globSeqNo]

", _tableName);

                        cmd.Parameters.Add("cutoff", SqlDbType.BigInt).Value = globalSequenceNumber;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                yield return ReadEvent(reader);
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

        static Event ReadEvent(IDataRecord reader)
        {
            var meta = (string) reader["meta"];
            var data = (byte[]) reader["data"];

            return Event.FromMetadata(JsonConvert.DeserializeObject<Metadata>(meta), data);
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
	    [meta] [nvarchar](max) NOT NULL,
	    [data] [varbinary](max) NOT NULL,

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
