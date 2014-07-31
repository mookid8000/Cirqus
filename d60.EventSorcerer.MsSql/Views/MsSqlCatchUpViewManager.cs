using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Serialization;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MsSql.Views
{
    public class MsSqlCatchUpViewManager<TView> : IViewManager where TView : IView, ISubscribeTo
    {
        readonly string _tableName;
        readonly Func<SqlConnection> _connectionProvider;
        readonly Action<SqlConnection> _cleanupAction;
        readonly Serializer _serializer = new Serializer("<events>");
        int _maxDomainEventsBetweenFlush;
        Prop[] _schema;

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

        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {

        }

        public TView Load(string viewId)
        {
            return default(TView);
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
                    var script = "[Id] [bigint] IDENTITY(1,1) NOT NULL, "
                        + Environment.NewLine
                                 + string.Join("," + Environment.NewLine, _schema
                                     .Where(c => !c.ColumnName.Equals("id", StringComparison.InvariantCultureIgnoreCase))
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

                    Console.WriteLine(cmd.CommandText);

                    //                    cmd.CommandText = string.Format(@"
                    //
                    //IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{0}')
                    //BEGIN
                    //    CREATE TABLE [dbo].[{0}] (
                    //        {1},
                    //	    [id] [bigint] IDENTITY(1,1) NOT NULL,
                    //	    [batchId] [uniqueidentifier] NOT NULL,
                    //	    [aggId] [uniqueidentifier] NOT NULL,
                    //	    [seqNo] [bigint] NOT NULL,
                    //	    [globSeqNo] [bigint] NOT NULL,
                    //	    [data] [nvarchar](max) NOT NULL,
                    //
                    //        CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
                    //        (
                    //	        [id] ASC
                    //        ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
                    //    )
                    //END
                    //
                    //", _tableName);

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