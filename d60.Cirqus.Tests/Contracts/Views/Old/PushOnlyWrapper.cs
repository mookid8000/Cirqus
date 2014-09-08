using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;

namespace d60.Cirqus.Tests.Contracts.Views.Old
{
    /// <summary>
    /// Wraps another view manager that is possibly both push/pull-based, ensuring that only the PUSH-capable API is used
    /// </summary>
    public class PushOnlyWrapper : IPushViewManager
    {
        readonly IPushViewManager _innerPushViewManager;

        public PushOnlyWrapper(IPushViewManager innerPushViewManager)
        {
            _innerPushViewManager = innerPushViewManager;
        }

        public void Initialize(IViewContext context, IEventStore eventStore, bool purgeExistingViews = false)
        {
            _innerPushViewManager.Initialize(context, eventStore, purgeExistingViews);
        }

        public bool Stopped
        {
            get { return _innerPushViewManager.Stopped; }
            set { _innerPushViewManager.Stopped = value; }
        }

        public void Dispatch(IViewContext context, IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            _innerPushViewManager.Dispatch(context, eventStore, events);
        }
    }
}