using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using Npgsql;
using NpgsqlTypes;

namespace d60.Cirqus.PostgreSql.Views
{
    public class PostgreSqlViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly string _tableName;
        private readonly Action<NpgsqlConnection> _additionalConnectionSetup;
        readonly string _positionTableName;
        const int PrimaryKeySize = 255;
        const int DefaultPosition = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
        readonly Logger _logger = CirqusLoggerFactory.Current.GetCurrentClassLogger();
        readonly string _connectionString;
        readonly GenericSerializer _serializer = new GenericSerializer();

        public PostgreSqlViewManager(string connectionStringOrConnectionStringName, string tableName, string positionTableName = null, bool automaticallyCreateSchema = true, Action<NpgsqlConnection> additionalConnectionSetup = null)
        {
            _tableName = tableName;
            _additionalConnectionSetup = additionalConnectionSetup;
            _positionTableName = positionTableName ?? _tableName + "_Position";
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
	""id"" VARCHAR({2}) NOT NULL,
	""data"" JSONB NOT NULL,
	PRIMARY KEY (""id"")
);

CREATE TABLE IF NOT EXISTS ""{1}"" (
	""id"" VARCHAR({2}) NOT NULL,
	""position"" BIGINT NOT NULL,
	PRIMARY KEY (""id"")
);

", _tableName, _positionTableName, PrimaryKeySize);

            _logger.Info("Ensuring that schema for '{0}' is created...", typeof(TViewInstance));

            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                command.ExecuteNonQuery();
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

        async Task<NpgsqlConnection> GetConnectionAsync()
        {
            var connection = new NpgsqlConnection(_connectionString);

            if (_additionalConnectionSetup != null)
                _additionalConnectionSetup.Invoke(connection);

            await connection.OpenAsync();

            return connection;
        }


        public override string Id
        {
            get { return string.Format("{0}/{1}", typeof (TViewInstance).GetPrettyName(), _tableName); }
        }

        public bool BatchDispatchEnabled { get; set; }

        public override async Task<long> GetPosition(bool canGetFromCache = true)
        {
            return await GetPositionFromPositionTable()
                   ?? DefaultPosition;
        }

        async Task<long?> GetPositionFromPositionTable()
        {
            using (var connection = await GetConnectionAsync())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"select ""position"" from ""{0}"" where ""id"" = @id", _positionTableName);
                command.Parameters.Add("id", NpgsqlDbType.Varchar, PrimaryKeySize).Value = _tableName;

                var result = await command.ExecuteScalarAsync();

                if (result == null || DBNull.Value == result)
                {
                    return null;
                }

                return Convert.ToInt64(result);
            }
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch, IViewManagerProfiler viewManagerProfiler)
        {
            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            var newPosition = eventList.Max(e => e.GetGlobalSequenceNumber());

            using (var connection = GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    var activeViewsById = new Dictionary<string, ActiveViewInstance>();

                    if (BatchDispatchEnabled)
                    {
                        var domainEventBatch = new DomainEventBatch(eventList);
                        eventList.Clear();
                        eventList.Add(domainEventBatch);
                    }

                    foreach (var e in eventList)
                    {
                        if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                        var stopwatch = Stopwatch.StartNew();
                        var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                        foreach (var viewId in viewIds)
                        {
                            var view = activeViewsById
                                .GetOrAdd(viewId, id =>
                                {
                                    var existing = FindOneById(id, connection, transaction);

                                    return existing != null 
                                        ? ActiveViewInstance.Existing(existing) 
                                        : ActiveViewInstance.New(_dispatcher.CreateNewInstance(id));
                                });

                            _dispatcher.DispatchToView(viewContext, e, view.ViewInstance);
                        }

                        viewManagerProfiler.RegisterTimeSpent(this, e, stopwatch.Elapsed);
                    }

                    Save(activeViewsById, connection, transaction);

                    RaiseUpdatedEventFor(activeViewsById.Values.Select(a => a.ViewInstance));

                    UpdatePosition(connection, transaction, newPosition);

                    transaction.Commit();
                }
            }
        }

        void UpdatePosition(NpgsqlConnection connection, NpgsqlTransaction transaction, long newPosition)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = string.Format(@"update ""{0}"" set ""position"" = @position where ""id"" = @id;", _positionTableName);
                
                command.Parameters.Add("id", NpgsqlDbType.Varchar, PrimaryKeySize).Value = _tableName;
                command.Parameters.Add("position", NpgsqlDbType.Bigint).Value = newPosition;

                var result = command.ExecuteNonQuery();

                if (result == 1) return;

                using (var insertCommand = connection.CreateCommand())
                {
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText = string.Format(@"insert into ""{0}"" (id, position) values (@id, @position);", _positionTableName);

                    insertCommand.Parameters.Add("id", NpgsqlDbType.Varchar, PrimaryKeySize).Value = _tableName;
                    insertCommand.Parameters.Add("position", NpgsqlDbType.Bigint).Value = newPosition;

                    var insertResult = insertCommand.ExecuteNonQuery();

                    if (insertResult != 1)
                    {
                        throw new ApplicationException(string.Format("Something went wrong when attempting to update the current position in {0}", _positionTableName));
                    }
                }
            }   
        }

        class ActiveViewInstance
        {
            ActiveViewInstance(TViewInstance viewInstance, bool isNew)
            {
                ViewInstance = viewInstance;
                IsNew = isNew;
            }

            public static ActiveViewInstance New(TViewInstance instance)
            {
                return new ActiveViewInstance(instance, true);
            }

            public static ActiveViewInstance Existing(TViewInstance instance)
            {
                return new ActiveViewInstance(instance, false);
            }

            public TViewInstance ViewInstance { get; private set; }
            public bool IsNew { get; private set; }
        }

        void Save(Dictionary<string, ActiveViewInstance> activeViewsById, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            if (!activeViewsById.Any()) return;

            _logger.Debug("Flushing {0} view instances to '{1}'", activeViewsById.Count, _tableName);

            var parametersAndData = activeViewsById
                .Select((kvp, index) => new
                {
                    Id = kvp.Key,
                    IsNew = kvp.Value.IsNew,
                    ViewInstance = kvp.Value.ViewInstance,
                    IdParameterName = "id" + index,
                    DataParameterName = "data" + index,
                })
                .ToList();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                var sqlCommands = parametersAndData
                    .Select(a => a.IsNew
                        ? string.Format(@"insert into ""{0}"" (id, data) values (@{1}, @{2}::jsonb);", _tableName,
                            a.IdParameterName, a.DataParameterName)
                        : string.Format(@"update ""{0}"" set data = @{2}::jsonb where id = @{1};", _tableName,
                            a.IdParameterName, a.DataParameterName))
                    .ToList();

                foreach (var a in parametersAndData)
                {
                    command.Parameters.Add(a.IdParameterName, NpgsqlDbType.Varchar, PrimaryKeySize).Value = a.Id;
                    command.Parameters.AddWithValue(a.DataParameterName, _serializer.Serialize(a.ViewInstance));
                }

                command.CommandText = string.Join(Environment.NewLine, sqlCommands);

                try
                {
                    var affectedRows = command.ExecuteNonQuery();

                    if (affectedRows != sqlCommands.Count)
                    {
                        throw new ApplicationException(
                            string.Format("Number of affected rows ({0}) did not match the expected number: {1}",
                                affectedRows, sqlCommands.Count));
                    }
                }
                catch (Exception exception)
                {
                    throw new ApplicationException(string.Format("Could not execute the following SQL: {0}", command.CommandText), exception);
                }
            }
        }

        TViewInstance FindOneById(string id, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                command.Parameters.Add("id", NpgsqlDbType.Varchar, PrimaryKeySize).Value = id;
                command.CommandText = string.Format(@"select ""data"" from ""{0}"" where ""id"" = @id", _tableName);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var data = reader["data"];
                    var jsonText = data.ToString();

                    return (TViewInstance)_serializer.Deserialize(jsonText);
                }
            }
        }

        public override void Purge()
        {
            _logger.Info("Purging PostgreSQL table {0}", _tableName);

            using (var connection = GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = string.Format(@"delete from ""{0}""", _tableName);
                        command.ExecuteNonQuery();
                    }

                    UpdatePosition(connection, transaction, DefaultPosition);

                    transaction.Commit();
                }
            }
        }

        public override TViewInstance Load(string viewId)
        {
            using (var connection = GetConnection())
            {
                return FindOneById(viewId, connection, null);
            }
        }

        public override void Delete(string viewId)
        {
        }
    }
}