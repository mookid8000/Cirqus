using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using d60.Cirqus.Caching;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// Simple caching event store decorator that caches all event that it comes by
    /// </summary>
    public class CachingEventStoreDecorator : IEventStore, IDisposable
    {
        static Logger _log;

        static CachingEventStoreDecorator()
        {
            CirqusLoggerFactory.Changed += f => _log = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry<EventData>>> _eventsPerAggregateRoot = new ConcurrentDictionary<string, ConcurrentDictionary<long, CacheEntry<EventData>>>();
        readonly ConcurrentDictionary<long, CacheEntry<EventData>> _eventsPerGlobalSequenceNumber = new ConcurrentDictionary<long, CacheEntry<EventData>>();
        readonly Timer _trimTimer = new Timer(30000);
        readonly IEventStore _innerEventStore;

        int _maxCacheEntries;

        public CachingEventStoreDecorator(IEventStore innerEventStore)
        {
            _innerEventStore = innerEventStore;

            _trimTimer.Elapsed += delegate
            {
                try
                {
                    PossiblyTrimCache();

                    _log.Debug("Status after cache check: currently holds {0} events by global sequence number and {1} aggregate root streams",
                        _eventsPerGlobalSequenceNumber.Count, _eventsPerAggregateRoot.Count);
                }
                catch (Exception exception)
                {
                    _log.Error(exception, "An error ocurred while trimming the event cache");
                }
            };

            _trimTimer.Start();
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
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _innerEventStore.GetNextGlobalSequenceNumber();
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            var eventsForThisAggregateRoot = _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry<EventData>>());
            var nextSequenceNumberToGet = firstSeq;

            CacheEntry<EventData> cacheEntry;
            while (eventsForThisAggregateRoot.TryGetValue(nextSequenceNumberToGet, out cacheEntry))
            {
                nextSequenceNumberToGet++;
                cacheEntry.MarkAsAccessed();
                yield return cacheEntry.Data;
            }

            foreach (var loadedEvent in _innerEventStore.Load(aggregateRootId, nextSequenceNumberToGet))
            {
                AddToCache(loadedEvent, eventsForThisAggregateRoot);

                yield return loadedEvent;
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            CacheEntry<EventData> cacheEntry;
            var nextGlobalSequenceNumberToGet = globalSequenceNumber;

            while (_eventsPerGlobalSequenceNumber.TryGetValue(nextGlobalSequenceNumberToGet, out cacheEntry))
            {
                nextGlobalSequenceNumberToGet++;
                cacheEntry.MarkAsAccessed();
                yield return cacheEntry.Data;
            }

            foreach (var loadedEvent in _innerEventStore.Stream(nextGlobalSequenceNumberToGet))
            {
                AddToCache(loadedEvent);
                yield return loadedEvent;
            }
        }

        public void Dispose()
        {
            _trimTimer.Stop();
            _trimTimer.Dispose();
        }

        void PossiblyTrimCache()
        {
            if (_eventsPerGlobalSequenceNumber.Count <= _maxCacheEntries) return;

            _log.Debug("Trimming caches");

            var entriesOldestFirst = _eventsPerGlobalSequenceNumber.Values
                .OrderByDescending(e => e.Age)
                .ToList();

            foreach (var entryToRemove in entriesOldestFirst)
            {
                if (_eventsPerGlobalSequenceNumber.Count <= _maxCacheEntries) break;

                CacheEntry<EventData> removedCacheEntry;

                _eventsPerGlobalSequenceNumber.TryRemove(entryToRemove.Data.GetGlobalSequenceNumber(), out removedCacheEntry);
                var eventsForThisAggregateRoot = GetEventsForThisAggregateRoot(entryToRemove.Data.GetAggregateRootId());
                eventsForThisAggregateRoot.TryRemove(entryToRemove.Data.GetSequenceNumber(), out removedCacheEntry);
            }
        }

        void AddToCache(EventData eventData, ConcurrentDictionary<long, CacheEntry<EventData>> eventsForThisAggregateRoot = null)
        {
            var aggregateRootId = eventData.GetAggregateRootId();

            if (eventsForThisAggregateRoot == null)
            {
                eventsForThisAggregateRoot = GetEventsForThisAggregateRoot(aggregateRootId);
            }

            eventsForThisAggregateRoot.AddOrUpdate(eventData.GetSequenceNumber(),
                seqNo => new CacheEntry<EventData>(eventData),
                (seqNo, existing) => existing.MarkAsAccessed());

            _eventsPerGlobalSequenceNumber.AddOrUpdate(eventData.GetGlobalSequenceNumber(),
                globSeqNo => new CacheEntry<EventData>(eventData),
                (globSeqNo, existing) => existing.MarkAsAccessed());
        }

        ConcurrentDictionary<long, CacheEntry<EventData>> GetEventsForThisAggregateRoot(string aggregateRootId)
        {
            return _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry<EventData>>());
        }
    }
}