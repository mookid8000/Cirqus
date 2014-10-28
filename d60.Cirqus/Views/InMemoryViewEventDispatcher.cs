using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// In-memory view that gets its events by having them dispatched directly
    /// </summary>
    public class InMemoryViewEventDispatcher<TViewInstance> : IEventDispatcher where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly ConcurrentDictionary<string, TViewInstance> _views = new ConcurrentDictionary<string, TViewInstance>();
        readonly ViewDispatcherHelper<TViewInstance> _dispatcher = new ViewDispatcherHelper<TViewInstance>();
        readonly ViewLocator _viewLocator = ViewLocator.GetLocatorFor<TViewInstance>();

        Logger _logger;
        bool _stopped;

        public InMemoryViewEventDispatcher(IAggregateRootRepository aggregateRootRepository, IDomainEventSerializer domainEventSerializer)
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
            _aggregateRootRepository = aggregateRootRepository;
            _domainEventSerializer = domainEventSerializer;
        }

        /// <summary>
        /// Can be used to configure this view event dispatcher to skip initialization - can be used when the history of the
        /// relevant events is not that important, when the view automatically gets warm after a while, etc.
        /// </summary>
        public bool SkipInitialization { get; set; }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            if (SkipInitialization)
            {
                _logger.Info("Skipping initialization of in-mem view event dispatcher for {0}", typeof(TViewInstance));
                return;
            }

            _logger.Info("Initializing in-mem view event dispatcher for {0}", typeof (TViewInstance));

            foreach (var batch in eventStore.Stream().Batch(1000))
            {
                if (_stopped)
                {
                    _logger.Warn("Event processing stopped during initialization... that was a bad start!");
                    return;
                }

                Dispatch(eventStore, batch);
            }
        }

        void Dispatch(IEventStore eventStore, IEnumerable<Event> events)
        {
            Dispatch(eventStore, events.Select(e => _domainEventSerializer.DoDeserialize(e)));
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            if (_stopped) return;

            try
            {
                var viewContext = new DefaultViewContext(_aggregateRootRepository);

                foreach (var e in events)
                {
                    try
                    {
                        if (!ViewLocator.IsRelevant<TViewInstance>(e)) continue;

                        var affectedViewIds = _viewLocator.GetAffectedViewIds(viewContext, e);

                        foreach (var viewId in affectedViewIds)
                        {
                            try
                            {
                                var viewInstance = _views.GetOrAdd(viewId, id => _dispatcher.CreateNewInstance(id));

                                _dispatcher.DispatchToView(viewContext, e, viewInstance);

                            }
                            catch (Exception exception)
                            {
                                throw new ApplicationException(string.Format("An error ocurred when dispatching {0} to view with ID {1}",
                                    e, viewId), exception);
                            }
                        }

                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(string.Format("Could not dispatch {0} to view(s)", e), exception);
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.Warn(exception, "An error occurred during event processing - the view will stop processing events!");
                _stopped = true;
            }
        }
    }
}