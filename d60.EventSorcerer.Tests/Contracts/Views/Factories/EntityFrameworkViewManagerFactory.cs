using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.EntityFramework;
using d60.EventSorcerer.MsSql;
using d60.EventSorcerer.Tests.MsSql;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    public class EntityFrameworkViewManagerFactory : IViewManagerFactory
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        public IViewManager GetViewManagerFor<TView>() where TView : class, IView, ISubscribeTo, new()
        {
            var connectionString = SqlHelper.GetConnectionString(TestSqlHelper.ConnectionStringName);

            TestSqlHelper.DropTable(connectionString, typeof(TView).Name);
            TestSqlHelper.DropTable(connectionString, typeof(TView).Name + "Configs");
            TestSqlHelper.DropTable(connectionString, "__MigrationHistory");

            var viewManager = new EntityFrameworkViewManager<TView>(TestSqlHelper.ConnectionStringName);
            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;
            _viewManagers.Add(viewManager);
            return viewManager;
        }

        public TView Load<TView>(string id) where TView : class, IView, ISubscribeTo, new()
        {
            var viewManager = _viewManagers.OfType<EntityFrameworkViewManager<TView>>().FirstOrDefault();

            if (viewManager == null)
            {
                throw new ApplicationException(string.Format("Could not find view manager for views of type {0}", typeof(TView)));
            }

            return viewManager.Load(id);

        }

        event Action<int> MaxDomainEventsBetweenFlushSet = delegate { };

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
            MaxDomainEventsBetweenFlushSet(value);
        }
    }
}