using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Testing
{
    /// <summary>
    /// In-memory view that gets its events by having them dispatched directly
    /// </summary>
    public class SynchronousViewManagerEventDispatcher : IEventDispatcher
    {
        readonly List<IViewManager> viewManagers;
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly IDomainTypeNameMapper _domainTypeNameMapper;
        readonly IDictionary<string, object> _viewContextItems = new Dictionary<string, object>();
        readonly Logger _logger = CirqusLoggerFactory.Current.GetCurrentClassLogger();

        public SynchronousViewManagerEventDispatcher(
            IAggregateRootRepository aggregateRootRepository,
            IDomainEventSerializer domainEventSerializer,
            IDomainTypeNameMapper domainTypeNameMapper,
            params IViewManager[] viewManagers)
        {
            this.viewManagers = viewManagers.ToList();

            _aggregateRootRepository = aggregateRootRepository;
            _domainEventSerializer = domainEventSerializer;
            _domainTypeNameMapper = domainTypeNameMapper;
        }

        public void SetContextItems(IDictionary<string, object> contextItems)
        {
            if (contextItems == null) throw new ArgumentNullException("contextItems");

            foreach (var kvp in contextItems)
            {
                _viewContextItems[kvp.Key] = kvp.Value;
            }
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            foreach (var batch in eventStore.Stream().Batch(1000))
            {
                Dispatch(batch.Select(e => _domainEventSerializer.Deserialize(e)));
            }
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            var context = new DefaultViewContext(_aggregateRootRepository, _domainTypeNameMapper, events);

            foreach (var kvp in _viewContextItems)
            {
                context.Items[kvp.Key] = kvp.Value;
            }

            var eventList = events.ToList();

            foreach (var viewManager in viewManagers)
            {
                var thisParticularPosition = viewManager.GetPosition().Result;
                if (thisParticularPosition >= eventList.Max(e => e.GetGlobalSequenceNumber())) continue;

                _logger.Debug("Dispatching batch of {0} events to {1}", eventList.Count, viewManager);

                viewManager.Dispatch(context, eventList, new NullProfiler());
            }
        }
    }
}