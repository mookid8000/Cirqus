using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public class InMemoryViewManager<TView> : IEnumerable<TView>, IViewManager where TView : class, IView, ISubscribeTo, new()
    {
        readonly ConcurrentDictionary<string, TView> _views = new ConcurrentDictionary<string, TView>();
        readonly ViewDispatcherHelper<TView> _viewDispatcherHelper = new ViewDispatcherHelper<TView>();

        public TView Load(string viewId)
        {
            TView view;

            return _views.TryGetValue(viewId, out view)
                ? view
                : null;
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExisting = false)
        {
            if (purgeExisting)
            {
                _views.Clear();
            }

            foreach (var e in eventStore.Stream())
            {
                Dispatch(context, eventStore, new[] { e });
            }
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                var viewId = ViewLocator.GetLocatorFor<TView>().GetViewId(e);

                _views.AddOrUpdate(viewId,
                    id =>
                    {
                        var view = new TView();
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