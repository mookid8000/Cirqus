using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.EntityFramework;
using d60.EventSorcerer.Tests.MsSql;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.Tests.Contracts.Views.Factories
{
    public class EntityFrameworkViewManagerFactory : IViewManagerFactory
    {
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();
        readonly string _connectionString = TestSqlHelper.ConnectionString;

        public EntityFrameworkViewManagerFactory()
        {
            Console.WriteLine("Dropping migration history");
            TestSqlHelper.DropTable("__MigrationHistory");
        }

        public IViewManager GetViewManagerFor<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            Console.WriteLine("Creating FRESH entity framework view manager for {0}", typeof(TView));

            TestSqlHelper.DropTable(typeof(TView).Name);
            TestSqlHelper.DropTable(typeof(TView).Name + "Configs");

            var viewManager = new EntityFrameworkViewManager<TView>(_connectionString);
            MaxDomainEventsBetweenFlushSet += maxEvents => viewManager.MaxDomainEventsBetweenFlush = maxEvents;
            _viewManagers.Add(viewManager);
            return viewManager;
        }

        public TView Load<TView>(string id) where TView : class, IViewInstance, ISubscribeTo, new()
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