using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

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
            
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            throw new NotImplementedException();
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            throw new NotImplementedException();
        }

        public override void Purge()
        {
            throw new NotImplementedException();
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