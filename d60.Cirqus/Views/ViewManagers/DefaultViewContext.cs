using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// View context implementation that works nicely with the existing view managers
    /// </summary>
    public class DefaultViewContext : IViewContext, IUnitOfWork
    {
        static Logger _logger;

        static DefaultViewContext()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly List<DomainEvent> _eventBatch;
        readonly RealUnitOfWork _realUnitOfWork;

        /// <summary>
        /// Creates the view context with the given repository and type name mapper, storing the given event batch to be able to look up events in case it could make sense
        /// </summary>
        public DefaultViewContext(IAggregateRootRepository aggregateRootRepository, IDomainTypeNameMapper domainTypeNameMapper, IEnumerable<DomainEvent> eventBatch)
        {
            Items = new Dictionary<string, object>();
            _aggregateRootRepository = aggregateRootRepository;
            _eventBatch = eventBatch.OrderBy(e => e.GetGlobalSequenceNumber()).ToList();
            _realUnitOfWork = new RealUnitOfWork(_aggregateRootRepository, domainTypeNameMapper);
        }

        public bool Exists(string aggregateRootId, long globalSequenceNumberCutoff)
        {
            var cachedInstance = GetFromCacheOrNull<AggregateRoot>(aggregateRootId, globalSequenceNumberCutoff);

            if (cachedInstance != null)
            {
                return true;
            }

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

            if (rootFromCache != null)
            {
                return rootFromCache;
            }

            var aggregateRoot = LoadAggregateRoot<TAggregateRoot>(aggregateRootId, globalSequenceNumber);

            _cachedRoots[aggregateRootId] = new CachedRoot(aggregateRoot, globalSequenceNumber);

            return aggregateRoot as TAggregateRoot;
        }

        AggregateRoot LoadAggregateRoot<TAggregateRoot>(string aggregateRootId, long globalSequenceNumber)
            where TAggregateRoot : class
        {
            var aggregateRootInfo = _aggregateRootRepository
                .Get<TAggregateRoot>(aggregateRootId, this, maxGlobalSequenceNumber: globalSequenceNumber);

            var aggregateRoot = aggregateRootInfo;

            var frozen = new FrozenAggregateRootService(aggregateRootInfo, _realUnitOfWork);
            aggregateRoot.UnitOfWork = frozen;
            return aggregateRoot;
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
                return (TAggregateRoot)entry.AggregateRoot;
            }

            return null;
        }

        class CachedRoot
        {
            readonly AggregateRootInfo _aggregateRootInfo;
            long _globalSequenceNumber;

            public CachedRoot(object aggregateRoot, long globalSequenceNumber)
            {
                AggregateRoot = aggregateRoot;
                IsOk = true;
                _globalSequenceNumber = globalSequenceNumber;
                _aggregateRootInfo = new AggregateRootInfo(aggregateRoot as AggregateRoot);
            }

            public object AggregateRoot { get; private set; }

            public bool IsOk { get; private set; }

            public void MaybeApply(DomainEvent domainEvent, RealUnitOfWork realUnitOfWork, long globalSequenceNumber)
            {
                var globalSequenceNumberFromEvent = domainEvent.GetGlobalSequenceNumber();

                // don't do anything if we are not supposed to go this far
                if (globalSequenceNumberFromEvent > globalSequenceNumber) return;

                // don't do anything if the event is in the past
                if (globalSequenceNumberFromEvent <= _globalSequenceNumber) return;

                // if this entry is a future version of the requested version, it's not ok.... sorry!
                if (_globalSequenceNumber > globalSequenceNumber)
                {
                    IsOk = false;
                    return;
                }

                // only apply event if it's ours...
                if (domainEvent.GetAggregateRootId() != _aggregateRootInfo.Id)
                {
                    return;
                }

                try
                {
                    var sequenceNumberFromEvent = domainEvent.GetSequenceNumber();
                    var expectedNextSequenceNumber = _aggregateRootInfo.SequenceNumber + 1;

                    if (expectedNextSequenceNumber != sequenceNumberFromEvent)
                    {
                        IsOk = false;
                        return;
                    }

                    _aggregateRootInfo.Apply(domainEvent, realUnitOfWork);

                    _globalSequenceNumber = globalSequenceNumberFromEvent;
                }
                catch (Exception exception)
                {
                    _logger.Warn(exception, "Got an error while bringing cache entry for {0} up-to-date to {1}",
                        _aggregateRootInfo.Id, globalSequenceNumber);

                    IsOk = false;
                }
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