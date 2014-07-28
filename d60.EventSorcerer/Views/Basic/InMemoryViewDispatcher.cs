using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public class InMemoryViewDispatcher<TView> : IEnumerable<TView>, IViewDispatcher where TView : IView, new()
    {
        readonly ConcurrentDictionary<string, TView> _views = new ConcurrentDictionary<string, TView>();
        readonly ViewDispatcher<TView> _viewDispatcher = new ViewDispatcher<TView>();

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            foreach (var e in events)
            {
                var viewId = ViewLocator.GetLocatorFor<TView>().GetViewId(e);

                _views.AddOrUpdate(viewId,
                    id =>
                    {
                        var view = new TView();
                        _viewDispatcher.DispatchToView(e, view);
                        return view;
                    },
                    (id, view) =>
                    {
                        _viewDispatcher.DispatchToView(e, view);
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