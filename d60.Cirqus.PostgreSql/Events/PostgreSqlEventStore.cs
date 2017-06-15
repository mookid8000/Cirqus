using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace d60.Cirqus.PostgreSql.Events
{
    public class PostgreSqlEventStore : IEventStore
    {
        readonly Action<NpgsqlConnection> _additionalConnectionSetup;
        readonly string _connectionString;
        readonly string _tableName;
        readonly MetadataSerializer _metadataSerializer = new MetadataSerializer();

        public PostgreSqlEventStore(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true, Action<NpgsqlConnection> additionalConnectionSetup = null)
        {
            _tableName = tableName;
            _connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);
            _additionalConnectionSetup = additionalConnectionSetup;

            if (automaticallyCreateSchema)
            {
                CreateSchema();
            }
        }

        void CreateSchema()
        {
            var sql = string.Format(@"

DO $$
BEGIN

IF NOT EXISTS (
    SELECT 1
    FROM   pg_class c
    JOIN   pg_namespace n ON n.oid = c.relnamespace
    WHERE  c.relname = '{0}'
    ) THEN

CREATE TABLE IF NOT EXISTS ""{0}"" (
	""id"" BIGSERIAL NOT NULL,
	""batchId"" UUID NOT NULL,
	""aggId"" VARCHAR(255) NOT NULL,
	""seqNo"" BIGINT NOT NULL,
	""globSeqNo"" BIGINT NOT NULL,
	""meta"" BYTEA NOT NULL,
	""data"" BYTEA NOT NULL,
	PRIMARY KEY (""id"")
);

CREATE UNIQUE INDEX ""Idx_{0}_aggId_seqNo"" ON ""{0}"" (""aggId"", ""seqNo"");
CREATE UNIQUE INDEX ""Idx_{0}_globSeqNo"" ON ""{0}"" (""globSeqNo""); 

END IF;

END$$;



", _tableName);

            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            var eventList = batch.ToList();

            try
            {
                using (var connection = GetConnection())
                using (var tx = connection.BeginTransaction())
                {
                    var nextSequenceNumber = GetNextGlobalSequenceNumber(connection, tx);

                    foreach (var e in eventList)
                    {
                        e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (nextSequenceNumber++).ToString(Metadata.NumberCulture);
                        e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
                    }

                    EventValidation.ValidateBatchIntegrity(batchId, eventList);

                    foreach (var e in eventList)
                    {
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.Transaction = tx;
                            cmd.CommandText = string.Format(@"

INSERT INTO ""{0}"" (
    ""batchId"",
    ""aggId"",
    ""seqNo"",
    ""globSeqNo"",
    ""data"",
    ""meta""
) VALUES (
    @batchId,
    @aggId,
    @seqNo,
    @globSeqNo,
    @data,
    @meta
)

", _tableName);


                            cmd.Parameters.AddWithValue("batchId", batchId);
                            cmd.Parameters.AddWithValue("aggId", e.GetAggregateRootId());
                            cmd.Parameters.AddWithValue("seqNo", NpgsqlDbType.Bigint, e.Meta[DomainEvent.MetadataKeys.SequenceNumber]);
                            cmd.Parameters.AddWithValue("globSeqNo", NpgsqlDbType.Bigint, e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber]);
                            cmd.Parameters.AddWithValue("data", e.Data);
                            cmd.Parameters.AddWithValue("meta", Encoding.UTF8.GetBytes(_metadataSerializer.Serialize(e.Meta)));

                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
            catch (PostgresException exception)
            {
                if (exception.SqlState == "23505")
                {
                    throw new ConcurrencyException(batchId, eventList, exception);
                }

                throw;
            }
        }

        long GetNextGlobalSequenceNumber(NpgsqlConnection conn, NpgsqlTransaction tx)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format(@"SELECT MAX(""globSeqNo"") FROM ""{0}""", _tableName);

                var result = cmd.ExecuteScalar();

                return result != DBNull.Value
                    ? (long)result + 1
                    : 0;
            }
        }

        NpgsqlConnection GetConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);

            if (_additionalConnectionSetup != null)
                _additionalConnectionSetup.Invoke(connection);

            connection.Open();

            return connection;
        }


        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;

                        cmd.CommandText = string.Format(@"SELECT ""data"", ""meta"" FROM ""{0}"" WHERE ""aggId"" = @aggId AND ""seqNo"" >= @firstSeqNo ORDER BY ""seqNo""", _tableName);
                        cmd.Parameters.AddWithValue("aggId", aggregateRootId);
                        cmd.Parameters.AddWithValue("firstSeqNo", NpgsqlDbType.Bigint, firstSeq);

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
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"

SELECT ""data"", ""meta"" FROM ""{0}"" WHERE ""globSeqNo"" >= @cutoff ORDER BY ""globSeqNo""", _tableName);

                        cmd.Parameters.AddWithValue("cutoff", NpgsqlDbType.Bigint, globalSequenceNumber);

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
        }

        EventData ReadEvent(IDataRecord reader)
        {
            var data = (byte[]) reader["data"];
            var meta = (byte[]) reader["meta"];

            return EventData.FromMetadata(_metadataSerializer.Deserialize(Encoding.UTF8.GetString(meta)), data);
        }

        public long GetNextGlobalSequenceNumber()
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    return GetNextGlobalSequenceNumber(connection, tx);
                }
            }
        }

        public void DropEvents()
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"DELETE FROM ""{0}""", _tableName);
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
        }
    }
}
