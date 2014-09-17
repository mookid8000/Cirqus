using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Views.ViewManagers.Old
{
    /// <summary>
    /// Event dispatcher that can dispatch events to any number of view managers based on <see cref="ILegacyViewManager"/>,
    /// either <see cref="IPushViewManager"/> or <see cref="IPullViewManager"/>.
    /// </summary>
    public class LegacyViewManagerEventDispatcher : IEventDispatcher, IEnumerable<ILegacyViewManager>
    {
        static Logger _logger;

        static LegacyViewManagerEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        public event Action<ILegacyViewManager, Exception> Error = delegate { };

        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<ILegacyViewManager> _viewManagers;

        public LegacyViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, params ILegacyViewManager[] viewManagers)
            : this(aggregateRootRepository, (IEnumerable<ILegacyViewManager>)viewManagers)
        {
        }

        public LegacyViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEnumerable<ILegacyViewManager> viewManagers)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _viewManagers = viewManagers.ToList();
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            var viewContext = new DefaultViewContext(_aggregateRootRepository);

            foreach (var manager in _viewManagers)
            {
                try
                {
                    _logger.Info("Initializing view manager {0}", manager);

                    manager.Initialize(viewContext, eventStore, purgeExistingViews: purgeExistingViews);

                    HandleViewManagerSuccess(manager);
                }
                catch (Exception exception)
                {
                    HandleViewManagerError(manager, exception);
                }
            }
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            var eventList = events.ToList();

            var viewContext = new DefaultViewContext(_aggregateRootRepository);

            foreach (var viewManager in _viewManagers)
            {
                try
                {
                    if (viewManager is IPushViewManager)
                    {
                        var pushViewManager = ((IPushViewManager)viewManager);

                        _logger.Debug("Dispatching {0} events directly to {1}", eventList.Count, viewManager);

                        pushViewManager.Dispatch(viewContext, eventStore, eventList);
                    }

                    if (viewManager is IPullViewManager)
                    {
                        var pullViewManager = ((IPullViewManager)viewManager);

                        var lastGlobalSequenceNumber = eventList.Last().GetGlobalSequenceNumber();

                        _logger.Debug("Asking {0} to catch up to {1}", viewManager, lastGlobalSequenceNumber);

                        pullViewManager.CatchUp(viewContext, eventStore, lastGlobalSequenceNumber);
                    }

                    HandleViewManagerSuccess(viewManager);
                }
                catch (Exception exception)
                {
                    HandleViewManagerError(viewManager, exception);
                }
            }
        }

        void HandleViewManagerSuccess(ILegacyViewManager manager)
        {
            if (manager.Stopped)
            {
                _logger.Info("View manager {0} was stopped, but it seems to have recovered and resumed with success", manager);
            }

            manager.Stopped = false;
        }

        void HandleViewManagerError(ILegacyViewManager viewManager, Exception exception)
        {
            _logger.Warn(exception, "An error occurred in the view manager {0} - setting Stopped = true", viewManager);

            viewManager.Stopped = true;

            Error(viewManager, exception);
        }

        internal void Add(ILegacyViewManager viewManager)
        {
            _viewManagers.Add(viewManager);
        }

        public IEnumerator<ILegacyViewManager> GetEnumerator()
        {
            return _viewManagers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}