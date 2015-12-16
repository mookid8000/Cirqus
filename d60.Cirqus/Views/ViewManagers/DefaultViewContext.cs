using System;
using System.Collections.Generic;
using System.Linq;
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
    public class DefaultViewContext : IViewContext, IUnitOfWork
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<DomainEvent> _eventBatch;
        readonly RealUnitOfWork _realUnitOfWork;

        public DefaultViewContext(IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper domainTypeNameMapper, IEnumerable<DomainEvent> eventBatch)
        {
            Items = new Dictionary<string, object>();

            _aggregateRootRepository = aggregateRootRepository;

            _eventBatch = eventBatch.ToList();
            _eventBatch.Sort((e1, e2) => e1.GetGlobalSequenceNumber().CompareTo(e2.GetGlobalSequenceNumber()));

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

        public event Action Committed;

        readonly Dictionary<string, CachedRoot> _cachedRoots = new Dictionary<string, CachedRoot>();

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            var rootFromCache = GetFromCacheOrNull<TAggregateRoot>(aggregateRootId, globalSequenceNumber);

            if (rootFromCache != null) return rootFromCache;

            var aggregateRootInfo = _aggregateRootRepository
                .Get<TAggregateRoot>(aggregateRootId, this, maxGlobalSequenceNumber: globalSequenceNumber);

            var aggregateRoot = aggregateRootInfo;

            var frozen = new FrozenAggregateRootService(aggregateRootInfo, _realUnitOfWork);
            aggregateRoot.UnitOfWork = frozen;

            _cachedRoots[aggregateRootId] = new CachedRoot
            {
                AggregateRoot = aggregateRoot,
                GlobalSequenceNumber = globalSequenceNumber,
                Id = aggregateRootId,
                IsOk = true
            };

            return aggregateRoot as TAggregateRoot;
        }

        TAggregateRoot GetFromCacheOrNull<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber) where TAggregateRoot : class
        {
            CachedRoot entry;

            if (!_cachedRoots.TryGetValue(aggregateRootId, out entry)) return null;

            foreach (var e in _eventBatch)
            {
                entry.MaybeApply(e, _realUnitOfWork, globalSequenceNumber);
            }

            if (entry.IsOk)
            {
                return (TAggregateRoot) entry.AggregateRoot;
            }

            return null;
        }

        class CachedRoot
        {
            public object AggregateRoot { get; set; }
            public string Id { get; set; }
            public long GlobalSequenceNumber { get; set; }

            public bool IsOk { get; set; }

            public void MaybeApply(DomainEvent domainEvent, RealUnitOfWork realUnitOfWork, long globalSequenceNumber)
            {
                var globalSequenceNumberFromEvent = domainEvent.GetGlobalSequenceNumber();

                // don't do anything if we are not supposed to go this far
                if (globalSequenceNumberFromEvent > globalSequenceNumber) return;

                // don't do anything if the event is in the past
                if (globalSequenceNumberFromEvent <= GlobalSequenceNumber) return;

                // only apply event if it's ours...
                if (domainEvent.GetAggregateRootId() != Id) return;

                var info = new AggregateRootInfo(AggregateRoot as AggregateRoot);

                var sequenceNumberFromEvent = domainEvent.GetSequenceNumber();

                var expectedNextSequenceNumber = info.SequenceNumber + 1;

                if (expectedNextSequenceNumber != sequenceNumberFromEvent)
                {
                    IsOk = false;
                    return;
                }

                info.Apply(domainEvent, realUnitOfWork);

                GlobalSequenceNumber = globalSequenceNumberFromEvent;
            }
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

            public void AddEmittedEvent<TAggregateRoot>(AggregateRoot aggregateRoot, DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
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

            public event Action Committed;
        }

        public DomainEvent CurrentEvent { get; set; }

        public Dictionary<string, object> Items { get; private set; }

        public void DeleteThisViewInstance()
        {

        }

        public void AddEmittedEvent<TAggregateRoot>(AggregateRoot aggregateRoot, DomainEvent<TAggregateRoot> e) where TAggregateRoot : AggregateRoot
        {
            throw new NotImplementedException("A view context cannot be used as a unit of work when emitting events");
        }

        public void AddToCache(AggregateRoot aggregateRoot, long globalSequenceNumberCutoff)
        {
            _realUnitOfWork.AddToCache(aggregateRoot, globalSequenceNumberCutoff);
        }
    }
}