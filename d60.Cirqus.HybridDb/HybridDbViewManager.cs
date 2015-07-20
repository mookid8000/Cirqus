using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using HybridDb;
using HybridDb.Commands;

namespace d60.Cirqus.HybridDb
{
    public class HybridDbViewManager<TViewInstance>  : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        const int DefaultPosition = -1;

        readonly IDocumentStore _store;
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();
        readonly ViewDispatcherHelper<TViewInstance> _dispatcherHelper = new ViewDispatcherHelper<TViewInstance>();

        readonly string viewPositionKey = typeof (TViewInstance).Name;

        bool _purging;

        public HybridDbViewManager(IDocumentStore store)
        {
            _store = store;
        }

        public override async Task<long> GetPosition(bool canGetFromCache = true)
        {
            using (var session = _store.OpenSession())
            {
                var position = session.Load<ViewPosition>(viewPositionKey);
                if (position != null)
                {
                    return position.Position;
                }
            }

            return DefaultPosition;
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch, IViewManagerProfiler viewManagerProfiler)
        {
            if (_purging) return;

            var eventList = batch.ToList();

            if (!eventList.Any()) return;

            using (var session = _store.OpenSession())
            {
                foreach (var e in eventList)
                {
                    if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                    var stopwatch = Stopwatch.StartNew();
                    var viewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                    foreach (var viewId in viewIds)
                    {
                        var viewInstance = session.Load<TViewInstance>(viewId);
                        
                        if (viewInstance == null)
                        {
                            viewInstance = _dispatcherHelper.CreateNewInstance(viewId);
                            session.Store(viewInstance);
                        }

                        _dispatcherHelper.DispatchToView(viewContext, e, viewInstance);
                    }

                    viewManagerProfiler.RegisterTimeSpent(this, e, stopwatch.Elapsed);
                }

                var position = session.Load<ViewPosition>(viewPositionKey);
                if (position == null)
                {
                    position = new ViewPosition { Id = viewPositionKey };
                    session.Store(position);
                }

                position.Position = eventList.Max(e => e.GetGlobalSequenceNumber());

                RaiseUpdatedEventFor(session.Advanced.ManagedEntities.OfType<TViewInstance>());

                session.SaveChanges();
            }
        }

        public override void Purge()
        {
            _purging = true;

            var commands = new List<DatabaseCommand>();
            var table = _store.Configuration.GetDesignFor<TViewInstance>().Table;

            QueryStats stats;
            foreach (var row in _store.Query(table, out stats, "id, etag"))
            {
                commands.Add(new DeleteCommand(table, (string)row["id"], (Guid)row["etag"], false));
            }

            var positionTable = _store.Configuration.GetDesignFor<ViewPosition>().Table;

            if (_store.Get(positionTable, viewPositionKey) != null)
            {
                commands.Add(new DeleteCommand(positionTable, viewPositionKey, Guid.Empty, true));
            }

            _store.Execute(commands);

            _purging = false;
        }

        public override TViewInstance Load(string viewId)
        {
            using (var session = _store.OpenSession())
            {
                return session.Load<TViewInstance>(viewId);
            }
        }

        public override void Delete(string viewId)
        {
            using (var session = _store.OpenSession())
            {
                var view = session.Load<TViewInstance>(viewId);
                session.Delete(view);
                session.SaveChanges();
            }
        }

        public class ViewPosition
        {
            public string Id { get; set; }
            public long Position { get; set; }
        }
    }
}
