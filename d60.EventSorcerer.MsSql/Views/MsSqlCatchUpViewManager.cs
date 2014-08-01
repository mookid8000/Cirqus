using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.Extensions;
using d60.EventSorcerer.Serialization;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MsSql.Views
{
    public class MsSqlCatchUpViewManager<TView> : IViewManager where TView : class, IView, ISubscribeTo, new()
    {
        const int PrimaryKeySize = 100;
        readonly string _tableName;
        readonly Func<SqlConnection> _connectionProvider;
        readonly Action<SqlConnection> _cleanupAction;
        readonly Serializer _serializer = new Serializer("<events>");
        readonly Prop[] _schema;
        readonly ViewDispatcherHelper<TView> _dispatcher = new ViewDispatcherHelper<TView>();
        int _maxDomainEventsBetweenFlush;

        public MsSqlCatchUpViewManager(string connectionStringOrConnectionStringName, string tableName, bool automaticallyCreateSchema = true)
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

        public void Initialize(IEventStore eventStore, bool purgeExisting = false)
        {
            if (purgeExisting)
            {
                Purge();
            }

            //var viewInstanceWithMaxGlobalSequenceNumber = _viewCollection
            //    .FindAllAs<MongoDbCatchUpView<TView>>()
            //    .SetSortOrder(SortBy<MongoDbCatchUpView<TView>>.Descending(v => v.MaxGlobalSeq))
            //    .SetLimit(1)
            //    .FirstOrDefault();

            //var globalSequenceNumberCutoff = viewInstanceWithMaxGlobalSequenceNumber == null
            //    ? 0
            //    : viewInstanceWithMaxGlobalSequenceNumber.MaxGlobalSeq + 1;

            //var batches = eventStore.Stream(globalSequenceNumberCutoff).Batch(1000);

            //foreach (var partition in batches)
            //{
            //    Dispatch(eventStore, partition);
            //}

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

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var eventsList = events.ToList();
            try
            {
                foreach (var batch in eventsList.Batch(MaxDomainEventsBetweenFlush))
                {
                    ProcessOneBatch(eventStore, batch);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                try
                {
                    // make sure we flush after each single domain event
                    foreach (var e in eventsList)
                    {
                        ProcessOneBatch(eventStore, new[] { e });
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

        void ProcessOneBatch(IEventStore eventStore, IEnumerable<DomainEvent> batch)
        {
            WithConnection(conn => ProcessOneBatch(eventStore, batch, conn));
        }

        void ProcessOneBatch(IEventStore eventStore, IEnumerable<DomainEvent> batch, SqlConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                var locator = ViewLocator.GetLocatorFor<TView>();
                var activeViewsById = new Dictionary<string, TView>();

                foreach (var e in batch)
                {
                    var viewId = locator.GetViewId(e);
                    var view = activeViewsById
                        .GetOrAdd(viewId, id => FindOneById(id, tx, conn)
                                                ?? new TView());

                    DispatchEvent(eventStore, e, view);
                }

                Save(activeViewsById, conn, tx);

                tx.Commit();
            }
        }

        void Save(Dictionary<string, TView> activeViewsById, SqlConnection conn, SqlTransaction tx)
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

USING (VALUES (@id)) AS foo(Id)

ON ViewTable.Id = foo.Id

WHEN MATCHED THEN

    UPDATE SET {1}

WHEN NOT MATCHED THEN

    INSERT ({2}) VALUES ({3})
    
;
", _tableName, FormatAssignments(_schema.Where(prop => !prop.IsPrimaryKey)), FormatColumnNames(_schema), FormatParameterNames(_schema));

                    cmd.Parameters.Add("Id", SqlDbType.NChar, PrimaryKeySize).Value = id;

                    foreach (var prop in _schema.Where(p => !p.IsPrimaryKey))
                    {
                        var value = prop.Getter(view);
                        
                        cmd.Parameters.AddWithValue(prop.SqlParameterName, value);
                    }

                    Console.WriteLine(cmd.CommandText);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        void DispatchEvent(IEventStore eventStore, DomainEvent domainEvent, TView view)
        {
            _dispatcher.DispatchToView(domainEvent, view);
        }

        TView FindOneById(string id, SqlTransaction tx, SqlConnection conn)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = string.Format(@"

SELECT 

{0}

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

        public TView Load(string viewId)
        {
            TView view = null;

            WithConnection(conn =>
            {
                using (var tx = conn.BeginTransaction())
                {
                    view = FindOneById(viewId, tx, conn);
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
}