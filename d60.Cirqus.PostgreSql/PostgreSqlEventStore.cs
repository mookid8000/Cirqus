using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using Npgsql;

namespace d60.Cirqus.PostgreSql
{
    public class PostgreSqlEventStore : IEventStore
    {
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer("<events>");
        readonly string _connectionString;
        readonly string _tableName;

        public PostgreSqlEventStore(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
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
	""aggId"" UUID NOT NULL,
	""seqNo"" BIGINT NOT NULL,
	""globSeqNo"" BIGINT NOT NULL,
	""data"" TEXT NOT NULL,
	PRIMARY KEY (""id"")
);

CREATE UNIQUE INDEX ""Idx_{0}_aggId_seqNo"" ON ""{0}"" (""aggId"", ""seqNo"");
CREATE UNIQUE INDEX ""Idx_{0}_globSeqNo"" ON ""{0}"" (""globSeqNo""); 

END IF;

END$$;



", _tableName);

            /*
             * 	    [id] [bigint] IDENTITY(1,1) NOT NULL,
	    [batchId] [uniqueidentifier] NOT NULL,
	    [aggId] [uniqueidentifier] NOT NULL,
	    [seqNo] [bigint] NOT NULL,
	    [globSeqNo] [bigint] NOT NULL,
	    [data] [nvarchar](max) NOT NULL,
*/

            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
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
                        e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = nextSequenceNumber++;
                        e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;
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
    ""data""
) VALUES (
    @batchId,
    @aggId,
    @seqNo,
    @globSeqNo,
    @data
)

", _tableName);


                            cmd.Parameters.AddWithValue("batchId", batchId);
                            cmd.Parameters.AddWithValue("aggId", e.GetAggregateRootId());
                            cmd.Parameters.AddWithValue("seqNo", e.Meta[DomainEvent.MetadataKeys.SequenceNumber]);
                            cmd.Parameters.AddWithValue("globSeqNo", e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber]);
                            cmd.Parameters.AddWithValue("data", _domainEventSerializer.Serialize(e));

                            cmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                }
            }
            catch (NpgsqlException exception)
            {
                if (exception.Code == "23505")
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

            connection.Open();

            return connection;
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;

                        cmd.CommandText = string.Format(@"SELECT ""data"" FROM ""{0}"" WHERE ""aggId"" = @aggId AND ""seqNo"" >= @firstSeqNo", _tableName);
                        cmd.Parameters.AddWithValue("aggId", aggregateRootId);
                        cmd.Parameters.AddWithValue("firstSeqNo", firstSeq);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var data = (string)reader["data"];

                                yield return _domainEventSerializer.Deserialize(data);
                            }
                        }
                    }

                    tx.Commit();
                }
            }
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            using (var connection = GetConnection())
            {
                using (var tx = connection.BeginTransaction())
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"

SELECT ""data"" FROM ""{0}"" WHERE ""globSeqNo"" >= @cutoff ORDER BY ""globSeqNo""", _tableName);

                        cmd.Parameters.AddWithValue("cutoff", globalSequenceNumber);

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
