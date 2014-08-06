using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Extensions;

namespace d60.EventSorcerer.Views.Basic
{
    public class BasicEventDispatcher : IEventDispatcher, IEnumerable<IViewManager>
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<IViewManager> _viewManagers;

        public BasicEventDispatcher(IAggregateRootRepository aggregateRootRepository, params IViewManager[] viewManagers)
            : this(aggregateRootRepository, (IEnumerable<IViewManager>)viewManagers)
        {
        }

        public BasicEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEnumerable<IViewManager> viewManagers)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _viewManagers = viewManagers.ToList();
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            foreach (var manager in _viewManagers)
            {
                try
                {
                    manager.Initialize(new DefaultViewContext(_aggregateRootRepository), eventStore, purgeExistingViews: purgeExistingViews);

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

            foreach (var viewManager in _viewManagers)
            {
                try
                {
                    if (viewManager is IDirectDispatchViewManager)
                    {
                        ((IDirectDispatchViewManager) viewManager).Dispatch(new DefaultViewContext(_aggregateRootRepository),
                            eventStore, eventList);
                    }

                    if (viewManager is ICatchUpViewManager)
                    {
                        ((ICatchUpViewManager) viewManager).CatchUp(new DefaultViewContext(_aggregateRootRepository),
                            eventStore, eventList.Last().GetGlobalSequenceNumber());
                    }

                    HandleViewManagerSuccess(viewManager);
                }
                catch (Exception exception)
                {
                    HandleViewManagerError(viewManager, exception);
                }
            }
        }

        void HandleViewManagerSuccess(IViewManager manager)
        {
            manager.Stopped = false;
        }

        void HandleViewManagerError(IViewManager viewManager, Exception exception)
        {
            viewManager.Stopped = true;
        }

        class DefaultViewContext : IViewContext
        {
            readonly IAggregateRootRepository _aggregateRootRepository;

            public DefaultViewContext(IAggregateRootRepository aggregateRootRepository)
            {
                _aggregateRootRepository = aggregateRootRepository;
            }

            public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber: globalSequenceNumber);
            }
        }

        internal void Add(IViewManager viewManager)
        {
            _viewManagers.Add(viewManager);
        }

        public IEnumerator<IViewManager> GetEnumerator()
        {
            return _viewManagers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}