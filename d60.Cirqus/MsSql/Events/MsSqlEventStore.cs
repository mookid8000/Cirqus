using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
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

        public void Save(Guid batchId, IEnumerable<EventData> events)
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

                        foreach (var batch in eventList.Batch(10))
                        {
                            var commandInfo = batch
                                .Select((@event, index) => new
                                {
                                    AggregateRootId = @event.Meta[DomainEvent.MetadataKeys.AggregateRootId],
                                    AggregateRootIdParameter = string.Format("aggId{0}", index),
                                    SequenceNumber = @event.Meta[DomainEvent.MetadataKeys.SequenceNumber],
                                    SequenceNumberParameter = string.Format("seqNo{0}", index),
                                    GlobalSequenceNumber = @event.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber],
                                    GlobalSequenceNumberParameter = string.Format("globSeqNo{0}", index),
                                    Meta = JsonConvert.SerializeObject(@event.Meta),
                                    MetaParameter = string.Format("meta{0}", index),
                                    Data = @event.Data,
                                    DataParameter = string.Format("data{0}", index),
                                })
                                .ToList();

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.Transaction = tx;
                                var sqlStatements = commandInfo.Select(s => string.Format(@"

INSERT INTO [{0}] (
    [batchId],
    [aggId],
    [seqNo],
    [globSeqNo],
    [meta],
    [data]
) VALUES (
    @batchId,
    @{1},
    @{2},
    @{3},
    @{4},
    @{5}
)

",
_tableName, 
s.AggregateRootIdParameter,
s.SequenceNumberParameter,
s.GlobalSequenceNumberParameter,
s.MetaParameter,
s.DataParameter));

                                cmd.CommandText = string.Join(Environment.NewLine, sqlStatements);

                                cmd.Parameters.Add("batchId", SqlDbType.UniqueIdentifier).Value = batchId;

                                foreach (var info in commandInfo)
                                {
                                    cmd.Parameters.Add(info.AggregateRootIdParameter, SqlDbType.NVarChar).Value = info.AggregateRootId;
                                    cmd.Parameters.Add(info.SequenceNumberParameter, SqlDbType.BigInt).Value = info.SequenceNumber;
                                    cmd.Parameters.Add(info.GlobalSequenceNumberParameter, SqlDbType.BigInt).Value = info.GlobalSequenceNumber;
                                    cmd.Parameters.Add(info.MetaParameter, SqlDbType.NVarChar).Value = info.Meta;
                                    cmd.Parameters.Add(info.DataParameter, SqlDbType.VarBinary).Value = info.Data;
                                }

                                cmd.ExecuteNonQuery();
                            }

                            //using (var cmd = conn.CreateCommand())
//                            {
//                                cmd.Transaction = tx;
//                                cmd.CommandText = string.Format(@"

//INSERT INTO [{0}] (
//    [batchId],
//    [aggId],
//    [seqNo],
//    [globSeqNo],
//    [meta],
//    [data]
//) VALUES (
//    @batchId,
//    @aggId,
//    @seqNo,
//    @globSeqNo,
//    @meta,
//    @data
//)

//", _tableName);
//                                cmd.Parameters.Add("batchId", SqlDbType.UniqueIdentifier).Value = batchId;
//                                cmd.Parameters.Add("aggId", SqlDbType.NVarChar).Value = @event.Meta[DomainEvent.MetadataKeys.AggregateRootId];
//                                cmd.Parameters.Add("seqNo", SqlDbType.BigInt).Value = @event.Meta[DomainEvent.MetadataKeys.SequenceNumber];
//                                cmd.Parameters.Add("globSeqNo", SqlDbType.BigInt).Value = @event.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber];
//                                cmd.Parameters.Add("meta", SqlDbType.NVarChar).Value = JsonConvert.SerializeObject(@event.Meta);
//                                cmd.Parameters.Add("data", SqlDbType.VarBinary).Value = @event.Data;

//                                cmd.ExecuteNonQuery();
//                            }
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

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
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

SELECT [meta],[data] FROM [{0}] WITH (NOLOCK) WHERE [aggId] = @aggId AND [seqNo] >= @firstSeqNo ORDER BY [seqNo]

", _tableName);
                        cmd.Parameters.Add("aggId", SqlDbType.VarChar).Value = aggregateRootId;
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

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
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

SELECT [meta],[data] FROM [{0}] WITH (NOLOCK) WHERE [globSeqNo] >= @cutoff ORDER BY [globSeqNo]

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

        static EventData ReadEvent(IDataRecord reader)
        {
            var meta = (string) reader["meta"];
            var data = (byte[]) reader["data"];

            return EventData.FromMetadata(JsonConvert.DeserializeObject<Metadata>(meta), data);
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
	    [aggId] [nvarchar](255) NOT NULL,
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
