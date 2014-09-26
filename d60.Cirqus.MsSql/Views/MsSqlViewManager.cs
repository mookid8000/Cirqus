using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.MsSql.Views
{
    public class MsSqlViewManager<TViewInstance> : IViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const int PrimaryKeySize = 100;
        const int DefaultPosition = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly string _connectionString;
        readonly string _tableName;
        readonly string _positionTableName;
        readonly Prop[] _viewTableSchema;

        readonly Prop[] _positionTableSchema =
        {
            new Prop {ColumnName = "Id", SqlDbType = SqlDbType.NVarChar},
            new Prop {ColumnName = "Position", SqlDbType = SqlDbType.BigInt},
        };

        Logger _logger;

        long _cachedPosition;

        public MsSqlViewManager(string connectionStringOrConnectionStringName, string tableName, string positionTableName = null, bool automaticallyCreateSchema = true)
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();

            _connectionString = SqlHelper.GetConnectionString(connectionStringOrConnectionStringName);
            _tableName = tableName;
            _positionTableName = positionTableName ?? tableName + "_Position";
            _viewTableSchema = SchemaHelper.GetSchema<TViewInstance>();

            if (automaticallyCreateSchema)
            {
                CreateSchema();
            }
        }

        public MsSqlViewManager(string connectionString, bool automaticallyCreateSchema = true)
            : this(connectionString, typeof(TViewInstance).Name, automaticallyCreateSchema: automaticallyCreateSchema)
        {
        }

        public event ViewInstanceUpdatedHandler<TViewInstance> Updated = delegate { };

        public long GetPosition(bool canGetFromCache = true)
        {
            if (canGetFromCache && false)
            {
                return GetPositionFromMemory()
                       ?? GetPositionFromViews()
                       ?? DefaultPosition;
            }

            return GetPositionFromPositionTable()
                   ?? GetPositionFromViews()
                   ?? DefaultPosition;
        }

        long? GetPositionFromMemory()
        {
            var value = Interlocked.Read(ref _cachedPosition);

            if (value != DefaultPosition)
            {
                return value;
            }

            return null;
        }

        long? GetPositionFromPositionTable()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format("SELECT [Position] FROM [{0}] WHERE Id = @id", _positionTableName);

                        cmd.Parameters.Add("id", SqlDbType.NVarChar, PrimaryKeySize).Value = _tableName;

                        var result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            return (long)result;
                        }
                    }
                }
            }

            return null;
        }

        long? GetPositionFromViews()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format("SELECT MAX([LastGlobalSequenceNumber]) FROM [{0}]", _tableName);

                        var result = cmd.ExecuteScalar();

                        if (result != DBNull.Value)
                        {
                            return (long)result;
                        }
                    }
                }
            }

            return null;
        }

        public void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            var newPosition = eventList.Max(e => e.GetGlobalSequenceNumber());

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    var locator = ViewLocator.GetLocatorFor<TViewInstance>();
                    var activeViewsById = new Dictionary<string, TViewInstance>();

                    foreach (var e in eventList)
                    {
                        if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                        var viewIds = locator.GetAffectedViewIds(viewContext, e);

                        foreach (var viewId in viewIds)
                        {
                            var view = activeViewsById
                                .GetOrAdd(viewId, id => FindOneById(id, tx, conn)
                                                        ?? _dispatcher.CreateNewInstance(viewId));

                            _dispatcher.DispatchToView(viewContext, e, view);
                        }
                    }

                    Save(activeViewsById, conn, tx);

                    RaiseEvents(activeViewsById.Values);

                    UpdatePosition(conn, tx, newPosition);

                    tx.Commit();
                }
            }

            Interlocked.Exchange(ref _cachedPosition, newPosition);
        }

        void RaiseEvents(IEnumerable<TViewInstance > viewInstances)
        {
            foreach (var instance in viewInstances)
            {
                Updated(instance);
            }
        }

        void UpdatePosition(SqlConnection conn, SqlTransaction tx, long newPosition)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                var commandText = string.Format(@"

MERGE [{0}] AS ViewTable

USING (VALUES (@Id)) AS foo(Id)

ON ViewTable.Id = foo.Id

WHEN MATCHED THEN

    UPDATE SET 
[Position] = @position

WHEN NOT MATCHED THEN

    INSERT (
[Id],
[Position]
) VALUES (
@id,
@position
)

;

", _positionTableName);
                
                cmd.CommandText = commandText;

                cmd.Parameters.Add("id", SqlDbType.NVarChar, PrimaryKeySize).Value = _tableName;
                cmd.Parameters.Add("position", SqlDbType.BigInt).Value = newPosition;

                cmd.ExecuteNonQuery();
            }

        }

        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GetNewPosition();

            var stopwatch = Stopwatch.StartNew();

            while (GetPosition(canGetFromCache: false) < mostRecentGlobalSequenceNumber)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        public TViewInstance Load(string viewId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    return FindOneById(viewId, tx, conn);
                }
            }
        }

        public void Purge()
        {
            _logger.Info("Purging SQL Server table {0}", _tableName);

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var tx = conn.BeginTransaction())
                {
                    DeleteRows(conn, tx, _tableName);

                    UpdatePosition(conn, tx, DefaultPosition);

                    tx.Commit();
                }
            }

            Interlocked.Exchange(ref _cachedPosition, DefaultPosition);
        }

        void DeleteRows(SqlConnection conn, SqlTransaction tx, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format(@"DELETE FROM [{0}]", tableName);
                cmd.ExecuteNonQuery();
            }
        }

        TViewInstance FindOneById(string viewId, SqlTransaction tx, SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format(@"

SELECT 

{0}

FROM [{1}] WHERE [Id] = @id

", FormatColumnNames(_viewTableSchema), _tableName);

                cmd.Parameters.Add("Id", SqlDbType.Char, PrimaryKeySize).Value = viewId;

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var view = _dispatcher.CreateNewInstance(viewId);

                        foreach (var prop in _viewTableSchema)
                        {
                            prop.Setter(view, reader[prop.ColumnName]);
                        }

                        return view;
                    }

                    return null;
                }
            }
        }

        static string FormatAssignments(IEnumerable<Prop> schema)
        {
            return string.Join(", " + Environment.NewLine,
                schema.Select(prop => string.Format("[{0}] = {1}", prop.ColumnName, prop.SqlParameterName)));
        }

        string FormatParameterNames(IEnumerable<Prop> schema)
        {
            return string.Join(", " + Environment.NewLine,
                schema.Select(prop => prop.SqlParameterName));
        }

        static string FormatColumnNames(IEnumerable<Prop> schema)
        {
            return string.Join(", " + Environment.NewLine,
                schema.Select(prop => string.Format("[{0}]", prop.ColumnName)));
        }

        void Save(Dictionary<string, TViewInstance> activeViewsById, SqlConnection conn, SqlTransaction tx)
        {
            _logger.Debug("Flushing {0} view instances to '{1}'", activeViewsById.Count, _tableName);

            foreach (var kvp in activeViewsById)
            {
                var id = kvp.Key;
                var view = kvp.Value;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = string.Format(@"

MERGE [{0}] AS ViewTable

USING (VALUES (@Id)) AS foo(Id)

ON ViewTable.Id = foo.Id

WHEN MATCHED THEN

    UPDATE SET 
{1}

WHEN NOT MATCHED THEN

    INSERT (
{2}
) VALUES (
{3}
)
    
;
", _tableName, FormatAssignments(_viewTableSchema.Where(prop => !prop.IsPrimaryKey)), FormatColumnNames(_viewTableSchema), FormatParameterNames(_viewTableSchema));

                    cmd.Parameters.Add("Id", SqlDbType.NChar, PrimaryKeySize).Value = id;

                    foreach (var prop in _viewTableSchema.Where(p => !p.IsPrimaryKey))
                    {
                        var value = prop.Getter(view);

                        cmd.Parameters.AddWithValue(prop.SqlParameterName, value ?? DBNull.Value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        void CreateSchema()
        {
            _logger.Info("Ensuring that schema for '{0}' is created...", typeof(TViewInstance));

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                if (!SchemaLooksRight(conn, _tableName, _viewTableSchema, _logger))
                {
                    DropTable(conn, _tableName);
                }

                CreateTable(conn, _tableName, _viewTableSchema);

                if (!SchemaLooksRight(conn, _positionTableName, _positionTableSchema, _logger))
                {
                    DropTable(conn, _positionTableName);
                }

                CreateTable(conn, _positionTableName, _positionTableSchema);
            }
        }

        static bool SchemaLooksRight(SqlConnection conn, string tableName, Prop[] targetSchema, Logger logger)
        {
            var columnsPresentInDatabase = new List<Prop>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format(@"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}'", tableName);

                var tableExists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;

                if (!tableExists)
                {
                    return true;
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format(@"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{0}'", tableName);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = (string)reader["COLUMN_NAME"];
                        var dataType = (string)reader["DATA_TYPE"];

                        var sqlDbType = (SqlDbType)Enum.Parse(typeof(SqlDbType), dataType, true);

                        columnsPresentInDatabase.Add(new Prop
                        {
                            ColumnName = columnName,
                            SqlDbType = sqlDbType
                        });
                    }
                }
            }

            var columnsPresentInSchemaMissingOrWithWrongTypeInDatabase = targetSchema
                .Where(schemaColumn => !columnsPresentInDatabase.Any(c => c.Matches(schemaColumn)))
                .ToList();

            if (columnsPresentInSchemaMissingOrWithWrongTypeInDatabase.Any())
            {
                logger.Warn("The table '{0}' is missing columns with the following names and types: {1}",
                    tableName, string.Join(", ", columnsPresentInSchemaMissingOrWithWrongTypeInDatabase.Select(prop => string.Format("{0} ({1})", prop.ColumnName, prop.SqlDbType))));

                return false;
            }

            return true;
        }

        static void DropTable(SqlConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = string.Format(@"
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    DROP TABLE [{0}]
END
", tableName);
                cmd.ExecuteNonQuery();
            }
        }

        static void CreateTable(SqlConnection conn, string tableName, Prop[] schema)
        {
            using (var cmd = conn.CreateCommand())
            {
                var script = string.Format("[Id] [NVARCHAR]({0}) NOT NULL, ", PrimaryKeySize)
                             + Environment.NewLine
                             + string.Join("," + Environment.NewLine, schema
                                 .Where(c => !c.IsPrimaryKey)
                                 .Select(c => string.Format("[{0}] [{1}]{2} {3}",
                                     c.ColumnName,
                                     c.SqlDbType,
                                     string.IsNullOrWhiteSpace(c.Size) ? "" : "(" + c.Size + ")",
                                     c.IsNullable ? "NULL" : "NOT NULL")));

                var commandText = string.Format(@"

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    CREATE TABLE [dbo].[{0}] (

{1},


        CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
        (
	        [id] ASC
        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )
END

", tableName, script);

                cmd.CommandText = commandText;

                cmd.ExecuteNonQuery();
            }
        }
    }
}