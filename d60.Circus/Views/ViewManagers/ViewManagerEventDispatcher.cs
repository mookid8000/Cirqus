using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.Circus.Aggregates;
using d60.Circus.Commands;
using d60.Circus.Events;
using d60.Circus.Extensions;

namespace d60.Circus.Views.ViewManagers
{
    /// <summary>
    /// Event dispatcher that can dispatch events to any number of view managers based on <see cref="IViewManager"/>,
    /// either <see cref="IPushViewManager"/> or <see cref="IPullViewManager"/>.
    /// </summary>
    public class ViewManagerEventDispatcher : IEventDispatcher, IEnumerable<IViewManager>
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<IViewManager> _viewManagers;

        public ViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, params IViewManager[] viewManagers)
            : this(aggregateRootRepository, (IEnumerable<IViewManager>)viewManagers)
        {
        }

        public ViewManagerEventDispatcher(IAggregateRootRepository aggregateRootRepository, IEnumerable<IViewManager> viewManagers)
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

            foreach (var viewManager in _viewManagers)
            {
                try
                {
                    if (viewManager is IPushViewManager)
                    {
                        ((IPushViewManager)viewManager).Dispatch(new DefaultViewContext(_aggregateRootRepository),
                            eventStore, eventList);
                    }

                    if (viewManager is IPullViewManager)
                    {
                        ((IPullViewManager)viewManager).CatchUp(new DefaultViewContext(_aggregateRootRepository),
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

        class DefaultViewContext : IViewContext, IUnitOfWork
        {
            readonly IAggregateRootRepository _aggregateRootRepository;
            readonly RealUnitOfWork _realUnitOfWork = new RealUnitOfWork();

            public DefaultViewContext(IAggregateRootRepository aggregateRootRepository)
            {
                _aggregateRootRepository = aggregateRootRepository;
            }

            public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
            {
                return _aggregateRootRepository
                    .Get<TAggregateRoot>(aggregateRootId, this, maxGlobalSequenceNumber: globalSequenceNumber)
                    .AggregateRoot;
            }

            public void AddEmittedEvent(DomainEvent e)
            {
                throw new NotImplementedException("A view context cannot be used as a unit of work when emitting events");
            }

            public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                return _realUnitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
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