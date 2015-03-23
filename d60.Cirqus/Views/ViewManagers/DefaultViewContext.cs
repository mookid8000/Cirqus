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

        public DefaultViewContext(IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper domainTypeNameMapper)
        {
            _aggregateRootRepository = aggregateRootRepository;

            _realUnitOfWork = new RealUnitOfWork(_aggregateRootRepository, domainTypeNameMapper);
        }

        public bool Exists(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            return _realUnitOfWork.Exists(aggregateRootId, globalSequenceNumberCutoff);
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists)
        {
            return _realUnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff, createIfNotExists);
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            var aggregateRootInfo = _aggregateRootRepository
                .Get<TAggregateRoot>(aggregateRootId, this, maxGlobalSequenceNumber: globalSequenceNumber);

            var aggregateRoot = aggregateRootInfo;

            var frozen = new FrozenAggregateRootService(aggregateRootInfo, _realUnitOfWork);
            aggregateRoot.UnitOfWork = frozen;

            return aggregateRoot as TAggregateRoot;
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
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

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            try
            {
                return Load<TAggregateRoot>(aggregateRootId);
            }
            catch
            {
                return null;
            }
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            try
            {
                return Load<TAggregateRoot>(aggregateRootId, globalSequenceNumber);
            }
            catch
            {
                return null;
            }
        }

        class FrozenAggregateRootService : IUnitOfWork
        {
            readonly AggregateRoot _aggregateRootInfo;
            readonly RealUnitOfWork _realUnitOfWork;

            public FrozenAggregateRootService(AggregateRoot aggregateRootInfo, RealUnitOfWork realUnitOfWork)
            {
                _aggregateRootInfo = aggregateRootInfo;
                _realUnitOfWork = realUnitOfWork;
            }

            public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
            {
                throw new InvalidOperationException(
                    string.Format("Aggregate root {0} with ID {1} attempted to emit event {2}, but that cannot be done when the root instance is frozen! (global sequence number: {3})",
                        typeof(TAggregateRoot), _aggregateRootInfo.Id, e, _aggregateRootInfo.GlobalSequenceNumberCutoff));
            }

            public void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff)
            {
                _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
            }

            public bool Exists(string aggregateRootId, long globalSequenceNumberCutoff)
            {
                return _realUnitOfWork.Exists(aggregateRootId, globalSequenceNumberCutoff);
            }

            public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, long globalSequenceNumberCutoff, bool createIfNotExists)
            {
                return _realUnitOfWork.Get<TAggregateRoot>(aggregateRootId, globalSequenceNumberCutoff, createIfNotExists: false);
            }
        }

        public DomainEvent CurrentEvent { get; set; }

        public void DeleteThisViewInstance()
        {

        }

        public void AddEmittedEvent<TAggregateRoot>(DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            throw new NotImplementedException("A view context cannot be used as a unit of work when emitting events");
        }

        public void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff)
        {
            _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
        }
    }
}