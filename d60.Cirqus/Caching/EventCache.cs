using System;
using System.Collections.Concurrent;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Caching
{
    /// <summary>
    /// Holds a number of cached events.
    /// </summary>
    public class EventCache
    {
        readonly ConcurrentDictionary<long, CacheEntry<DomainEvent>> _domainEventsBySequenceNumber
            = new ConcurrentDictionary<long, CacheEntry<DomainEvent>>();

        readonly ConcurrentDictionary<long, CacheEntry<EventData>> _entriesByGlobalSequenceNumber
            = new ConcurrentDictionary<long, CacheEntry<EventData>>();

        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry<EventData>>> _entriesByAggregateRoot
            = new ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry<EventData>>>();

        public int MaxEntries { get; set; }

        public EventData GetCachedEvent(long globalSequenceNumber)
        {
            CacheEntry<EventData> cacheEntry;

            return _entriesByGlobalSequenceNumber.TryGetValue(globalSequenceNumber, out cacheEntry)
                ? cacheEntry.Data
                : null;
        }

        public EventData GetCachedEvent(string aggregateRootId, long sequenceNumber)
        {
            CacheEntry<EventData> cacheEntry;

            return _entriesByAggregateRoot
                .GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry<EventData>>())
                .TryGetValue(sequenceNumber, out cacheEntry)
                ? cacheEntry.Data
                : null;
        }

        public DomainEvent GetDomainEvent(long globalSequenceNumber)
        {
            CacheEntry<DomainEvent> domainEvent;

            return _domainEventsBySequenceNumber.TryGetValue(globalSequenceNumber, out domainEvent)
                ? domainEvent.Data
                : null;
        }

        public void AddToCache(EventData eventData)
        {
            var globalSequenceNumber = eventData.GetGlobalSequenceNumber();
            var aggregateRootId = eventData.GetAggregateRootId();
            var sequenceNumber = eventData.GetSequenceNumber();

            _entriesByGlobalSequenceNumber
                .AddOrUpdate(globalSequenceNumber,
                    id => new CacheEntry<EventData>(eventData),
                    (id, entry) => entry.MarkAsAccessed());

            _entriesByAggregateRoot
                .GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry<EventData>>())
                .AddOrUpdate(sequenceNumber,
                    id => new CacheEntry<EventData>(eventData),
                    (id, entry) => entry.MarkAsAccessed());

            if (eventData.HasDomainEvent)
            {
                var domainEvent = eventData.GetDomainEvent();

                AddToCache(domainEvent);
            }


            if (!MaxEntriesExceeded()) return;
            
            Scavenge();
        }

        bool MaxEntriesExceeded()
        {
            return _entriesByGlobalSequenceNumber.Count > MaxEntries;
        }

        volatile bool _scavenging;

        void Scavenge()
        {
            if (_scavenging) return;

            lock (this)
            {
                if (_scavenging) return;

                _scavenging = true;

                try
                {
                    while (MaxEntriesExceeded())
                    {
                        RemoveLeastUsedCacheEntry();
                    }

                    AdjustAggregateRootCache();

                    AdjustDomainEventCache();
                }
                finally
                {
                    _scavenging = false;
                }
            }
        }

        void AdjustDomainEventCache()
        {
            foreach (var domainEvent in _domainEventsBySequenceNumber.Values.ToList())
            {
                if (_entriesByGlobalSequenceNumber.ContainsKey(domainEvent.Data.GetGlobalSequenceNumber())) continue;

                CacheEntry<DomainEvent> dummy;

                _domainEventsBySequenceNumber.TryRemove(domainEvent.Data.GetGlobalSequenceNumber(), out dummy);
            }
        }

        void AdjustAggregateRootCache()
        {
            // first, remove events in the aggregate root index if it's not present in the global index
            foreach (var eventsForAnAggregateRoot in _entriesByAggregateRoot.Values)
            {
                foreach (var e in eventsForAnAggregateRoot.Values.ToList())
                {
                    if (_entriesByGlobalSequenceNumber.ContainsKey(e.Data.GetGlobalSequenceNumber())) continue;

                    CacheEntry<EventData> dummy;

                    eventsForAnAggregateRoot.TryRemove(e.Data.GetSequenceNumber(), out dummy);
                }
            }

            // remove empty dictionaries
            var keysToRemove = _entriesByAggregateRoot
                .Where(d => d.Value.Count == 0)
                .Select(d => d.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                ConcurrentDictionary<long, CacheEntry<EventData>> dummy;
                _entriesByAggregateRoot.TryRemove(key, out dummy);
            }
        }

        void RemoveLeastUsedCacheEntry()
        {
            var entryToRemove = _entriesByGlobalSequenceNumber.Values.ToList()
                .OrderByDescending(e => e.Age)
                .FirstOrDefault();

            if (entryToRemove == null) return;

            CacheEntry<EventData> dummy;

            _entriesByGlobalSequenceNumber.TryRemove(entryToRemove.Data.GetGlobalSequenceNumber(), out dummy);
        }

        public void AddToCache(DomainEvent domainEvent)
        {
            _domainEventsBySequenceNumber
                .AddOrUpdate(domainEvent.GetGlobalSequenceNumber(),
                    id => new CacheEntry<DomainEvent>(domainEvent), 
                    (id, existing) => existing.MarkAsAccessed());
        }

        class CacheEntry<T>
        {
            public CacheEntry(T data)
            {
                Data = data;
                MarkAsAccessed();
            }

            public DateTime LastAccess { get; private set; }

            public T Data { get; private set; }

            public CacheEntry<T> MarkAsAccessed()
            {
                LastAccess = DateTime.UtcNow;
                return this;
            }

            public TimeSpan Age
            {
                get { return DateTime.UtcNow - LastAccess; }
            }
        }
    }
}