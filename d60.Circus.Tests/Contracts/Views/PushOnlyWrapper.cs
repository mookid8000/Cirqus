using System.Collections.Generic;
using d60.Circus.Events;
using d60.Circus.Views.Basic;

namespace d60.Circus.Tests.Contracts.Views
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