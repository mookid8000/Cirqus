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
        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, EventData>> _eventsPerAggregateRoot = new ConcurrentDictionary<string, ConcurrentDictionary<long, EventData>>();
        readonly ConcurrentDictionary<long, EventData> _eventsPerGlobalSequenceNumber = new ConcurrentDictionary<long, EventData>();

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
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _innerEventStore.GetNextGlobalSequenceNumber();
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            var eventsForThisAggregateRoot = _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, EventData>());
            var nextSequenceNumberToGet = firstSeq;

            EventData cachedEvent;
            while (eventsForThisAggregateRoot.TryGetValue(nextSequenceNumberToGet, out cachedEvent))
            {
                nextSequenceNumberToGet++;

                yield return cachedEvent;
            }

            foreach (var loadedEvent in _innerEventStore.Load(aggregateRootId, nextSequenceNumberToGet))
            {
                // WTF=!=!=!!??
                if (loadedEvent.GetSequenceNumber() < nextSequenceNumberToGet)
                {
                    continue;
                }
                AddToCache(loadedEvent, eventsForThisAggregateRoot);
                yield return loadedEvent;
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            EventData cachedEvent;
            while (_eventsPerGlobalSequenceNumber.TryGetValue(globalSequenceNumber++, out cachedEvent))
            {
                yield return cachedEvent;
            }

            foreach (var loadedEvent in _innerEventStore.Stream(globalSequenceNumber))
            {
                AddToCache(loadedEvent);
                yield return loadedEvent;
            }
        }

        void AddToCache(EventData eventData, ConcurrentDictionary<long, EventData> eventsForThisAggregateRoot = null)
        {
            var aggregateRootId = eventData.GetAggregateRootId();

            if (eventsForThisAggregateRoot == null)
            {
                eventsForThisAggregateRoot = _eventsPerAggregateRoot.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, EventData>());
            }

            eventsForThisAggregateRoot[eventData.GetSequenceNumber()] = eventData;
            _eventsPerGlobalSequenceNumber[eventData.GetGlobalSequenceNumber()] = eventData;

            // pretty primitive for now...
            if (_eventsPerGlobalSequenceNumber.Count > _maxCacheEntries)
            {
                _eventsPerGlobalSequenceNumber.Clear();
                _eventsPerAggregateRoot.Clear();
            }
        }
    }
}