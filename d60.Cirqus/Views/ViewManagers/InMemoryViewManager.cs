using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// In-memory catch-up view manager that can be used when your command processing happens on multiple machines
    /// or if you want your in-mem views to be residing on another machine than the one that does the command processing.
    /// </summary>
    public class InMemoryViewManager<TViewInstance> : AbstractViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ConcurrentDictionary<string, TViewInstance> _views = new ConcurrentDictionary<string, TViewInstance>();
        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();

        long _position = -1;

        public override TViewInstance Load(string viewId)
        {
            TViewInstance instance;

            return _views.TryGetValue(viewId, out instance)
                ? instance
                : null;
        }

        public override void Delete(string viewId)
        {
            Console.WriteLine("Deleting view {0}", viewId);
            TViewInstance dummy;
            _views.TryRemove(viewId, out dummy);
        }

        public override long GetPosition(bool canGetFromCache = true)
        {
            return InnerGetPosition();
        }

        public override void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch)
        {
            var updatedViews = new HashSet<TViewInstance>();

            foreach (var e in batch)
            {
                if (ViewLocator.IsRelevant<TViewInstance>(e))
                {
                    var affectedViewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                    foreach (var viewId in affectedViewIds)
                    {
                        var viewInstance = _views.GetOrAdd(viewId, id => _dispatcher.CreateNewInstance(id));

                        _dispatcher.DispatchToView(viewContext, e, viewInstance);

                        updatedViews.Add(viewInstance);
                    }
                }

                Interlocked.Exchange(ref _position, e.GetGlobalSequenceNumber());
            }

            RaiseUpdatedEventFor(updatedViews);
        }

        public override void Purge()
        {
            _views.Clear();
            Interlocked.Exchange(ref _position, -1);
        }

        long InnerGetPosition()
        {
            return Interlocked.Read(ref _position);
        }
    }
}