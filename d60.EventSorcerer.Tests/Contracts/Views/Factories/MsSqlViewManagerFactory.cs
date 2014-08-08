using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.MsSql.Views;
using d60.EventSorcerer.Tests.MsSql;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    public class MsSqlViewManagerFactory : IViewManagerFactory
    {
        readonly string _connectionString;
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        public MsSqlViewManagerFactory()
        {
            TestSqlHelper.EnsureTestDatabaseExists();

            _connectionString = TestSqlHelper.ConnectionString;
        }

        public IViewManager GetViewManagerFor<TView>() where TView : class, IView, ISubscribeTo, new()
        {
            var tableName = typeof(TView).Name;

            TestSqlHelper.DropTable(tableName);

            var viewManager = new MsSqlViewManager<TView>(_connectionString, tableName);

            _viewManagers.Add(viewManager);

            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;

            return viewManager;
        }

        public TView Load<TView>(string id) where TView : class, IView, ISubscribeTo, new()
        {
            var msSqlCatchUpViewManager = _viewManagers.OfType<MsSqlViewManager<TView>>().FirstOrDefault();

            if (msSqlCatchUpViewManager == null)
            {
                throw new ApplicationException(string.Format("Could not find a view manager of type {0}", typeof(MsSqlViewManager<TView>)));
            }

            return msSqlCatchUpViewManager.Load(id);
        }

        event Action<int> MaxDomainEventsBetweenFlushSet = delegate { }; 

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            MaxDomainEventsBetweenFlushSet(value);
        }
    }
}