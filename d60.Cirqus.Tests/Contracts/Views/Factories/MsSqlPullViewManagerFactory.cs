using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.MsSql.Views;
using d60.Cirqus.Tests.MsSql;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Tests.Contracts.Views.Factories
{
    public class MsSqlPullViewManagerFactory : IPullViewManagerFactory, IPushViewManagerFactory
    {
        readonly string _connectionString;
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        public MsSqlPullViewManagerFactory()
        {
            TestSqlHelper.EnsureTestDatabaseExists();

            _connectionString = TestSqlHelper.ConnectionString;
        }

        public IPullViewManager GetPullViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetMsSqlViewManager<TView>();

            return new PullOnlyWrapper(viewManager);
        }

        public IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = GetMsSqlViewManager<TView>();

            return new PushOnlyWrapper(viewManager);
        }

        MsSqlViewManager<TView> GetMsSqlViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var tableName = typeof (TView).Name;

            TestSqlHelper.DropTable(tableName);

            var viewManager = new MsSqlViewManager<TView>(_connectionString, tableName);

            _viewManagers.Add(viewManager);

            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;
            return viewManager;
        }

        public TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var msSqlCatchUpViewManager = _viewManagers.OfType<MsSqlViewManager<TView>>().FirstOrDefault();

            if (msSqlCatchUpViewManager == null)
            {
                throw new ApplicationException(string.Format("Could not find a view manager of type {0}", typeof(MsSqlViewManager<TView>)));
            }

            return msSqlCatchUpViewManager.Load(viewId);
        }

        event Action<int> MaxDomainEventsBetweenFlushSet = delegate { }; 

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            MaxDomainEventsBetweenFlushSet(value);
        }
    }
}