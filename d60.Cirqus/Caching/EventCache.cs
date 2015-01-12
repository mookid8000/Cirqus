using System;
using System.Collections.Concurrent;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Caching
{
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

            if (_entriesByGlobalSequenceNumber.Count > MaxEntries)
            {
                
            }
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