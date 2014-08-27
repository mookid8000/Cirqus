using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using Npgsql;
using NpgsqlTypes;

namespace d60.Cirqus.PostgreSql
{
    public class PostgreSqlEventStore : IEventStore
    {
        readonly string _tableName;
        readonly string _connectionString;
        readonly Serializer _serializer = new Serializer("<events>");

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

CREATE TABLE IF NOT EXISTS ""{0}"" (
	""id"" BIGSERIAL NOT NULL,
	""batchId"" UUID NOT NULL,
	""aggId"" UUID NOT NULL,
	""seqNo"" BIGINT NOT NULL,
	""globSeqNo"" BIGINT NOT NULL,
	""data"" JSONB NOT NULL,
	PRIMARY KEY (""id"")
);
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
            using (var connection = GetConnection())
            using (var tx = connection.BeginTransaction())
            {
                var eventList = batch.ToList();

                var nextSequenceNumber = GetNextSequenceNumber(connection, tx);

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
                        cmd.Parameters.AddWithValue("data", _serializer.Serialize(e));

                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }

        long GetNextSequenceNumber(NpgsqlConnection conn, NpgsqlTransaction tx)
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

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = Int32.MaxValue)
        {
            throw new NotImplementedException();
        }

        public long GetNextSeqNo(Guid aggregateRootId)
        {
            return 0;
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            throw new NotImplementedException();
        }
    }
}
