using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Caching
{
    public class CachingEventStoreDecorator : IEventStore 
    {
        readonly IEventStore _innerEventStore;
        readonly EventCache _eventCache;

        public CachingEventStoreDecorator(IEventStore innerEventStore, EventCache eventCache)
        {
            _innerEventStore = innerEventStore;
            _eventCache = eventCache;
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            var eventList = batch.ToList();

            _innerEventStore.Save(batchId, eventList);

            foreach (var e in eventList)
            {
                _eventCache.AddToCache(e);
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _innerEventStore.GetNextGlobalSequenceNumber();
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            EventData eventData;

            while ((eventData = _eventCache.GetCachedEvent(aggregateRootId, firstSeq++)) != null)
            {
                yield return eventData;
            }

            foreach (var loadedEvent in _innerEventStore.Load(aggregateRootId, firstSeq))
            {
                _eventCache.AddToCache(loadedEvent);
                
                yield return loadedEvent;
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            EventData eventData;

            while ((eventData = _eventCache.GetCachedEvent(globalSequenceNumber++)) != null)
            {
                yield return eventData;
            }

            foreach (var loadedEvent in _innerEventStore.Stream(globalSequenceNumber))
            {
                _eventCache.AddToCache(loadedEvent);

                yield return loadedEvent;
            }
        }
    }
}