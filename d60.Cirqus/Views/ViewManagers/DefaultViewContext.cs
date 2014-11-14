using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
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

        public DefaultViewContext(IAggregateRootRepository aggregateRootRepository, IDomainTypeMapper domainTypeMapper)
        {
            _aggregateRootRepository = aggregateRootRepository;

            _realUnitOfWork = new RealUnitOfWork(_aggregateRootRepository, domainTypeMapper);
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
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

        public bool Exists<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            return _aggregateRootRepository.Exists<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRootInfo<TAggregateRoot> Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists) where TAggregateRoot : AggregateRoot, new()
        {
            return _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, _realUnitOfWork, globalSequenceNumberCutoff);
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
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

            public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
            {
                throw new InvalidOperationException(
                    string.Format("Aggregate root {0} with ID {1} attempted to emit event {2}, but that cannot be done when the root instance is frozen! (global sequence number: {3})",
                        typeof(TAggregateRoot), _aggregateRootInfo.AggregateRoot.Id, e, _aggregateRootInfo.LastGlobalSeqNo));
            }

            public void AddToCache<TAggregateRootToAdd>(TAggregateRootToAdd aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRootToAdd : AggregateRoot
            {
                _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
            }

            public bool Exists<TAggregateRootToCheck>(string aggregateRootId, long globalSequenceNumberCutoff) where TAggregateRootToCheck : AggregateRoot
            {
                return _realUnitOfWork.Exists<TAggregateRootToCheck>(aggregateRootId, globalSequenceNumberCutoff);
            }

            public AggregateRootInfo<TAggregateRootToLoad> Get<TAggregateRootToLoad>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists) where TAggregateRootToLoad : AggregateRoot, new()
            {
                return _realUnitOfWork.Get<TAggregateRootToLoad>(aggregateRootId, globalSequenceNumberCutoff);
            }
        }

        public DomainEvent CurrentEvent { get; set; }

        public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            throw new NotImplementedException("A view context cannot be used as a unit of work when emitting events");
        }

        public void AddToCache<TAggregateRoot>(TAggregateRoot aggregateRoot, long globalSequenceNumberCutoff) where TAggregateRoot : AggregateRoot
        {
            _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
        }
    }
}