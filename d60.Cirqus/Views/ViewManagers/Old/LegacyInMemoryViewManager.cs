using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Views.ViewManagers.Old
{
    public class LegacyInMemoryViewManager<TView> : IEnumerable<TView>, IPushViewManager where TView : class, IViewInstance, ISubscribeTo, new()
    {
        readonly ConcurrentDictionary<string, TView> _views = new ConcurrentDictionary<string, TView>();
        readonly ViewDispatcherHelper<TView> _viewDispatcherHelper = new ViewDispatcherHelper<TView>();
        
        bool _initialized;

        public TView Load(string viewId)
        {
            TView view;

            return _views.TryGetValue(viewId, out view)
                ? view
                : null;
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (purgeExistingViews)
            {
                _views.Clear();
            }

            foreach (var e in eventStore.Stream().Batch(100))
            {
                Dispatch(context, eventStore, e);
            }

            _initialized = true;
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            if (!_initialized)
            {
                var message =
                    string.Format("The view manager for {0} has not been initialized! Please make sure that the view" +
                                  " manager is properly initialized, either by initializing it manually, or by having" +
                                  " the event dispatcher do it (which is the preferred way when you\'re working with" +
                                  " an event dispatcher)",
                        typeof (TView));

                throw new InvalidOperationException(message);
            }

            foreach (var e in events)
            {
                var viewLocator = ViewLocator.GetLocatorFor<TView>();

                if (!ViewLocator.IsRelevant<TView>(e)) continue;

                var viewIds = viewLocator.GetVirewIds(context, e);

                foreach (var viewId in viewIds)
                {
                    _views.AddOrUpdate(viewId,
                        id =>
                        {
                            var view = new TView
                            {
                                Id = id,
                                LastGlobalSequenceNumber = -1
                            };
                            _viewDispatcherHelper.DispatchToView(context, e, view);
                            return view;
                        },
                        (id, view) =>
                        {
                            _viewDispatcherHelper.DispatchToView(context, e, view);
                            return view;
                        });
                }
            }
        }

        public bool Stopped { get; set; }

        public IEnumerator<TView> GetEnumerator()
        {
            return _views.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}