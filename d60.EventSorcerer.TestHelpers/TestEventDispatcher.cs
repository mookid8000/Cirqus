using System;
using System.Collections.Generic;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.TestHelpers
{
    internal class TestEventDispatcher : IEventDispatcher
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<IViewManager> _viewManagers = new List<IViewManager>();

        public TestEventDispatcher(IAggregateRootRepository aggregateRootRepository)
        {
            _aggregateRootRepository = aggregateRootRepository;
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            foreach (var manager in _viewManagers)
            {
                manager.Initialize(new TestViewContext(_aggregateRootRepository), eventStore, purgeExistingViews);
            }
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            foreach (var manager in _viewManagers)
            {
                manager.Dispatch(new TestViewContext(_aggregateRootRepository), eventStore, events);
            }
        }

        public void AddViewManager(IViewManager viewManager)
        {
            _viewManagers.Add(viewManager);
        }

        class TestViewContext : IViewContext
        {
            readonly IAggregateRootRepository _aggregateRootRepository;

            public TestViewContext(IAggregateRootRepository aggregateRootRepository)
            {
                _aggregateRootRepository = aggregateRootRepository;
            }

            public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
            {
                return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber: globalSequenceNumber);
            }
        }
    }
}