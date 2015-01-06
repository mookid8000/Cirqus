using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Should work now....
    /// </summary>
    public class CachingEventStoreDecorator : IEventStore 
    {
        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry>> _eventsPerAggregateRoot = new ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry>>();
        readonly ConcurrentDictionary<long, CacheEntry> _eventsPerGlobalSequenceNumber = new ConcurrentDictionary<long, CacheEntry>();

        readonly IEventStore _innerEventStore;
        int _maxCacheEntries;

        public CachingEventStoreDecorator(IEventStore innerEventStore)
        {
            _innerEventStore = innerEventStore;
        }

        public int MaxCacheEntries
        {
            get { return _maxCacheEntries; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(string.Format("Cannot set max cache entries to {0} - the value must be greater than or equal to 0!", value));
                }
                _maxCacheEntries = value;
            }
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            var eventList = batch.ToList();

            _innerEventStore.Save(batchId, eventList);

            foreach (var e in eventList)
            {
                AddToCache(e);
            }

            PossiblyTrimCache();
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _innerEventStore.GetNextGlobalSequenceNumber();
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            var eventsForThisAggregateRoot = _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());
            var nextSequenceNumberToGet = firstSeq;

            CacheEntry cacheEntry;
            while (eventsForThisAggregateRoot.TryGetValue(nextSequenceNumberToGet, out cacheEntry))
            {
                nextSequenceNumberToGet++;
                cacheEntry.MarkAsAccessed();
                yield return cacheEntry.EventData;
            }

            foreach (var loadedEvent in _innerEventStore.Load(aggregateRootId, nextSequenceNumberToGet))
            {            
                AddToCache(loadedEvent, eventsForThisAggregateRoot);
                
                yield return loadedEvent;
            }

            PossiblyTrimCache();
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            CacheEntry cacheEntry;
            var nextGlobalSequenceNumberToGet = globalSequenceNumber;

            while (_eventsPerGlobalSequenceNumber.TryGetValue(nextGlobalSequenceNumberToGet, out cacheEntry))
            {
                nextGlobalSequenceNumberToGet++;
                cacheEntry.MarkAsAccessed();
                yield return cacheEntry.EventData;
            }

            foreach (var loadedEvent in _innerEventStore.Stream(nextGlobalSequenceNumberToGet))
            {
                AddToCache(loadedEvent);
                yield return loadedEvent;
            }
            
            PossiblyTrimCache();
        }

        void PossiblyTrimCache()
        {
            if (_eventsPerGlobalSequenceNumber.Count <= _maxCacheEntries) return;

            var entriesOldestFirst = _eventsPerGlobalSequenceNumber.Values
                .OrderByDescending(e => e.Age)
                .ToList();

            foreach (var entryToRemove in entriesOldestFirst)
            {
                if (_eventsPerGlobalSequenceNumber.Count <= _maxCacheEntries) break;

                CacheEntry removedCacheEntry;

                _eventsPerGlobalSequenceNumber.TryRemove(entryToRemove.EventData.GetGlobalSequenceNumber(), out removedCacheEntry);
                var eventsForThisAggregateRoot = GetEventsForThisAggregateRoot(entryToRemove.EventData.GetAggregateRootId());
                eventsForThisAggregateRoot.TryRemove(entryToRemove.EventData.GetSequenceNumber(), out removedCacheEntry);
            }
        }

        void AddToCache(EventData eventData, ConcurrentDictionary<long, CacheEntry> eventsForThisAggregateRoot = null)
        {
            var aggregateRootId = eventData.GetAggregateRootId();

            if (eventsForThisAggregateRoot == null)
            {
                eventsForThisAggregateRoot = GetEventsForThisAggregateRoot(aggregateRootId);
            }

            eventsForThisAggregateRoot.AddOrUpdate(eventData.GetSequenceNumber(),
                seqNo => new CacheEntry(eventData),
                (seqNo, existing) => existing.MarkAsAccessed());

            _eventsPerGlobalSequenceNumber.AddOrUpdate(eventData.GetGlobalSequenceNumber(),
                globSeqNo => new CacheEntry(eventData),
                (globSeqNo, existing) => existing.MarkAsAccessed());
        }

        ConcurrentDictionary<long, CacheEntry> GetEventsForThisAggregateRoot(string aggregateRootId)
        {
            return _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());
        }

        class CacheEntry
        {
            public CacheEntry(EventData eventData)
            {
                EventData = eventData;
                MarkAsAccessed();
            }

            public DateTime LastAccess { get; private set; }
            
            public EventData EventData { get; private set; }

            public CacheEntry MarkAsAccessed()
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