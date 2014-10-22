using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// View context implementation that works nicely with the existing view managers
    /// </summary>
    class DefaultViewContext : IViewContext, IUnitOfWork
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly RealUnitOfWork _realUnitOfWork;

        public DefaultViewContext(IAggregateRootRepository aggregateRootRepository)
        {
            _aggregateRootRepository = aggregateRootRepository;

            _realUnitOfWork = new RealUnitOfWork(_aggregateRootRepository);
        }

        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            if (CurrentEvent == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to load aggregate root {0} with ID {1} in snapshot at the time of the current event, but there was no current event on the context!",
                        typeof(TAggregateRoot), aggregateRootId));
            }

            return Load<TAggregateRoot>(aggregateRootId, CurrentEvent.GetGlobalSequenceNumber());
        }

        public bool Exists<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            return _aggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot, new()
        {
            return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, _realUnitOfWork, globalSequenceNumberCutoff);
        }

        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            if (!_aggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, maxGlobalSequenceNumber: globalSequenceNumber))
            {
                throw new ArgumentException(string.Format("Aggregate root {0} with ID {1} does not exist!", typeof(TAggregateRoot), aggregateRootId), "aggregateRootId");
            }

            var aggregateRootInfo = _aggregateRootRepository
                .Get<TAggregateRoot>(aggregateRootId, this, maxGlobalSequenceNumber: globalSequenceNumber);

            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            var frozen = new FrozenAggregateRootService<TAggregateRoot>(aggregateRootInfo, _realUnitOfWork);
            aggregateRoot.UnitOfWork = frozen;

            return aggregateRoot;
        }

        class FrozenAggregateRootService<TAggregateRoot> : IUnitOfWork where TAggregateRoot : AggregateRoot, new()
        {
            readonly AggregateRootInfo<TAggregateRoot> _aggregateRootInfo;
            readonly RealUnitOfWork _realUnitOfWork;

            public FrozenAggregateRootService(AggregateRootInfo<TAggregateRoot> aggregateRootInfo, RealUnitOfWork realUnitOfWork)
            {
                _aggregateRootInfo = aggregateRootInfo;
                _realUnitOfWork = realUnitOfWork;
            }

            public void AddEmittedEvent(DomainEvent e)
            {
                throw new InvalidOperationException(
                    string.Format("Aggregate root {0} with ID {1} attempted to emit event {2}, but that cannot be done when the root instance is frozen! (global sequence number: {3})",
                        typeof(TAggregateRoot), _aggregateRootInfo.AggregateRoot.Id, e, _aggregateRootInfo.LastGlobalSeqNo));
            }

            public TAggregateRoot GetAggregateRootFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                return _realUnitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
            {
                _realUnitOfWork.AddToCache<TAggregateRoot>(aggregateRoot, globalSequenceNumberCutoff);
            }

            public bool Exists<TAggregateRoot1>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot1 : AggregateRoot
            {
                return _realUnitOfWork.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public AggregateRootInfo<TAggregateRootToLoad> Get<TAggregateRootToLoad>(Guid aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRootToLoad : AggregateRoot, new()
            {
                return _realUnitOfWork.Get<TAggregateRootToLoad>(aggregateRootId, globalSequenceNumberCutoff);
            }
        }

        public DomainEvent CurrentEvent { get; set; }

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
}