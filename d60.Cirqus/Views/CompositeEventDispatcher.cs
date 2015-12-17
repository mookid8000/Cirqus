using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Event dispatcher that can contain multiple event dispatchers
    /// </summary>
    public class CompositeEventDispatcher : IAwaitableEventDispatcher
    {
        readonly List<IEventDispatcher> _eventDispatchers;

        public CompositeEventDispatcher(params IEventDispatcher[] eventDispatchers)
            :this((IEnumerable<IEventDispatcher>)eventDispatchers)
        {
        }

        public CompositeEventDispatcher(IEnumerable<IEventDispatcher> eventDispatchers)
        {
            _eventDispatchers = eventDispatchers.ToList();
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _eventDispatchers.ForEach(d => d.Initialize(eventStore, purgeExistingViews));
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            _eventDispatchers.ForEach(d => d.Dispatch(events));
        }

        public Task WaitUntilProcessed<TViewInstance>(CommandProcessingResult result, TimeSpan timeout) where TViewInstance : IViewInstance
        {
            return Task.WhenAll(_eventDispatchers.OfType<IAwaitableEventDispatcher>().Select(x => x.WaitUntilProcessed<TViewInstance>(result, timeout)));
        }

        public Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            return Task.WhenAll(_eventDispatchers.OfType<IAwaitableEventDispatcher>().Select(x => x.WaitUntilProcessed(result, timeout)));
        }
    }
}