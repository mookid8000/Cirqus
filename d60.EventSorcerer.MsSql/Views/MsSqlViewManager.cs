using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MsSql.Views
{
    public class MsSqlViewManager<TView> : IPushViewManager, IPullViewManager where TView : class, IViewInstance, ISubscribeTo, new()
    {
        const int PrimaryKeySize = 100;
        readonly string _tableName;
        readonly Func<SqlConnection> _connectionProvider;
        readonly Action<SqlConnection> _cleanupAction;
        readonly Prop[] _schema;
        readonly ViewDispatcherHelper<TView> _dispatcher = new ViewDispatcherHelper<TView>();
        int _maxDomainEventsBetweenFlush;

        public MsSqlViewManager(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
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

            _schema = SchemaHelper.GetSchema<TView>();

            if (automaticallyCreateSchema)
            {
                CreateSchema();
            }
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (purgeExistingViews)
            {
                Purge();
            }

            CatchUp(context, eventStore, long.MaxValue);
        }

        public void CatchUp(IViewContext context, IEventStore eventStore, long lastGlobalSequenceNumber)
        {
            var maxSequenceNumber = GetMaxSequenceNumber();
            var globalSequenceNumberCutoff = maxSequenceNumber + 1;

            var batches = eventStore.Stream(globalSequenceNumberCutoff).Batch(1000);

            foreach (var partition in batches)
            {
                Dispatch(context, eventStore, partition);
            }
        }

        public bool Stopped { get; set; }

        long GetMaxSequenceNumber()
        {
            var max = -1L;

            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format("SELECT MAX([GlobalSeqNo]) FROM [{0}]", _tableName);

                        var result = cmd.ExecuteScalar();

                        max = result != DBNull.Value
                            ? (long)result
                            : -1L;
                    }
                }
            });

            return max;
        }

        public void Purge()
        {
            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = string.Format(@"DELETE FROM [{0}]", _tableName);
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var maxGlobalSequenceNumber = GetMaxSequenceNumber();

            var eventsToDispatch = events
                .Where(e => e.GetGlobalSequenceNumber() > maxGlobalSequenceNumber)
                .ToList();

            try
            {
                foreach (var batch in eventsToDispatch.Batch(MaxDomainEventsBetweenFlush))
                {
                    ProcessOneBatch(eventStore, batch, context);
                }
            }
            catch (Exception)
            {
                try
                {
                    // make sure we flush after each single domain event
                    foreach (var e in eventsToDispatch)
                    {
                        ProcessOneBatch(eventStore, new[] { e }, context);
                    }
                }
                catch (ConsistencyException)
                {
                    throw;
                }
                catch (Exception)
                {
                }
            }
        }

        void ProcessOneBatch(IEventStore eventStore, IEnumerable<DomainEvent> batch, IViewContext context)
        {
            WithConnection(conn => ProcessOneBatch(eventStore, batch, conn, context));
        }

        void ProcessOneBatch(IEventStore eventStore, IEnumerable<DomainEvent> batch, SqlConnection conn, IViewContext context)
        {
            using (var tx = conn.BeginTransaction())
            {
                var locator = ViewLocator.GetLocatorFor<TView>();
                var activeViewsById = new Dictionary<string, MsSqlView<TView>>();

                foreach (var e in batch)
                {
                    if (!ViewLocator.IsRelevant<TView>(e)) continue;

                    var viewId = locator.GetViewId(e);
                    var view = activeViewsById
                        .GetOrAdd(viewId, id => FindOneById(id, tx, conn)
                                                ?? new MsSqlView<TView> { View = new TView() });

                    DispatchEvent(eventStore, e, view, context);
                }

                Save(activeViewsById, conn, tx);

                tx.Commit();
            }
        }

        void Save(Dictionary<string, MsSqlView<TView>> activeViewsById, SqlConnection conn, SqlTransaction tx)
        {
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
{1},
[GlobalSeqNo] = @GlobalSeqNo

WHEN NOT MATCHED THEN

    INSERT (
{2},
[GlobalSeqNo]
) VALUES (
{3},
@GlobalSeqNo
)
    
;
", _tableName, FormatAssignments(_schema.Where(prop => !prop.IsPrimaryKey)), FormatColumnNames(_schema), FormatParameterNames(_schema));

                    cmd.Parameters.Add("Id", SqlDbType.NChar, PrimaryKeySize).Value = id;
                    cmd.Parameters.Add("GlobalSeqNo", SqlDbType.BigInt).Value = view.MaxGlobalSeq;

                    foreach (var prop in _schema.Where(p => !p.IsPrimaryKey))
                    {
                        var value = prop.Getter(view.View);

                        cmd.Parameters.AddWithValue(prop.SqlParameterName, value);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
        }

        void DispatchEvent(IEventStore eventStore, DomainEvent domainEvent, MsSqlView<TView> view, IViewContext context)
        {
            var globalSequenceNumber = domainEvent.GetGlobalSequenceNumber();
            if (globalSequenceNumber < view.MaxGlobalSeq) return;

            _dispatcher.DispatchToView(context, domainEvent, view.View);

            view.MaxGlobalSeq = globalSequenceNumber;
        }

        MsSqlView<TView> FindOneById(string id, SqlTransaction tx, SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format(@"

SELECT 

{0},
[GlobalSeqNo]

FROM [{1}] WHERE [Id] = @id

", FormatColumnNames(_schema), _tableName);

                cmd.Parameters.Add("Id", SqlDbType.Char, PrimaryKeySize).Value = id;

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var view = new TView();

                        foreach (var prop in _schema)
                        {
                            prop.Setter(view, reader[prop.ColumnName]);
                        }

                        var globalSeqNo = (long)reader["GlobalSeqNo"];

                        return new MsSqlView<TView>
                        {
                            View = view,
                            MaxGlobalSeq = globalSeqNo
                        };
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

        public TView Load(string viewId)
        {
            TView view = null;

            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    var wrapper = FindOneById(viewId, tx, conn);

                    if (wrapper != null)
                    {
                        view = wrapper.View;
                    }
                }
            });

            return view;
        }

        public int MaxDomainEventsBetweenFlush
        {
            get { return _maxDomainEventsBetweenFlush; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException(string.Format("Attempted to set max events between flush to {0}, but it must be greater than 0!", value));
                }
                _maxDomainEventsBetweenFlush = value;
            }
        }

        void CreateSchema()
        {
            WithConnection(conn =>
            {
                using (var cmd = conn.CreateCommand())
                {
                    var script = string.Format("[Id] [NCHAR]({0}) NOT NULL, ", PrimaryKeySize)
                                 + Environment.NewLine
                                 + string.Join("," + Environment.NewLine, _schema
                                     .Where(c => !c.IsPrimaryKey)
                                     .Select(c => string.Format("[{0}] [{1}]{2} NOT NULL", c.ColumnName, c.SqlDbType, string.IsNullOrWhiteSpace(c.Size) ? "" : "(" + c.Size + ")")));

                    var commandText = string.Format(@"

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
BEGIN
    CREATE TABLE [dbo].[{0}] (

{1},

[GlobalSeqNo] [BigInt],


        CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
        (
	        [id] ASC
        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    )
END

", _tableName, script);

                    cmd.CommandText = commandText;

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

    public class ColumnAttribute : Attribute
    {
        public string ColumnName { get; private set; }

        public ColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }

    class MsSqlView<TView> where TView : IViewInstance
    {
        public long MaxGlobalSeq { get; set; }
        public TView View { get; set; }
    }
}