using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using Npgsql;
using NpgsqlTypes;

namespace d60.Cirqus.PostgreSql.Views
{
    public class PostgreSqlViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly string _tableName;
        readonly string _positionTableName;
        const int PrimaryKeySize = 100;
        const int DefaultPosition = -1;

        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
        readonly Logger _logger = CirqusLoggerFactory.Current.GetCurrentClassLogger();
        readonly string _connectionString;

        public PostgreSqlViewManager(string connectionStringOrConnectionStringName, string tableName, string positionTableName = null, bool automaticallyCreateSchema = true)
        {
            _tableName = tableName;
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
	""id"" VARCHAR(255) NOT NULL,
	""data"" JSONB NOT NULL,
	PRIMARY KEY (""id"")
);

CREATE TABLE IF NOT EXISTS ""{1}"" (
	""id"" VARCHAR(255) NOT NULL,
	""position"" BIGINT NOT NULL,
	PRIMARY KEY (""id"")
);

", _tableName, _positionTableName);

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

            connection.Open();

            return connection;
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            using (var connection = GetConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"select ""position"" from ""{0}"" where ""id"" = @id", _positionTableName);
                command.Parameters.Add("id", NpgsqlDbType.Varchar, 255).Value = _tableName;

                var result = command.ExecuteScalar();

                return Convert.ToInt64(result);
            }
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            var newPosition = eventList.Max(e => e.GetGlobalSequenceNumber());

            using (var connection = GetConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    var activeViewsById = new Dictionary<string, TViewInstance>();

                    foreach (var e in eventList)
                    {
                        if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                        var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                        foreach (var viewId in viewIds)
                        {
                            var view = activeViewsById
                                .GetOrAdd(viewId, id => FindOneById(id, transaction, connection)
                                                        ?? _dispatcher.CreateNewInstance(viewId));

                            _dispatcher.DispatchToView(viewContext, e, view);
                        }
                    }

                    Save(activeViewsById, connection, transaction);

                    RaiseUpdatedEventFor(activeViewsById.Values);

                    UpdatePosition(connection, transaction, newPosition);

                    transaction.Commit();
                }
            }
        }

        void UpdatePosition(NpgsqlConnection connection, NpgsqlTransaction transaction, long newPosition)
        {
                
        }


        void Save(Dictionary<string, TViewInstance> activeViewsById, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var parametersAndData = activeViewsById
                .Select((kvp, index) => new
                {
                    Id = kvp.Key,
                    ViewInstance = kvp.Value,
                    IdParameterName = "id" + index,
                    DataParameterName = "data" + index,
                })
                .ToList();

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;


            }
        }

        TViewInstance FindOneById(string id, NpgsqlTransaction transaction, NpgsqlConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.Parameters.Add("id", NpgsqlDbType.Varchar, 255).Value = id;
                command.CommandText = string.Format(@"select ""data"" from ""{0}"" where ""id"" = @id", _tableName);

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var data = reader["data"];

                    data.ToString();

                    return null;
                }
            }
        }

        public override void Purge()
        {
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

                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = string.Format(@"delete from ""{0}"" where ""id"" = @id", _positionTableName);
                        command.Parameters.Add("id", NpgsqlDbType.Varchar, 255).Value = _tableName;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        public override TViewInstance Load(string viewId)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string viewId)
        {
            throw new NotImplementedException();
        }
    }
}