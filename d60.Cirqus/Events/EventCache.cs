using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Events
{
    public class EventCache : IEventStore 
    {
        readonly IEventStore store;
        readonly int maxEntries;
        readonly ConcurrentDictionary<string, ConcurrentDictionary<long, EventData>> cache; 

        public EventCache(IEventStore store, int maxEntries = 100000)
        {
            this.store = store;
            this.maxEntries = maxEntries;

            cache = new ConcurrentDictionary<string, ConcurrentDictionary<long, EventData>>();
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            store.Save(batchId, batch);
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            var cacheForRoot = cache.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, EventData>());

            EventData cachedEvent;
            while(cacheForRoot.TryGetValue(firstSeq, out cachedEvent))
            {
                yield return cachedEvent;
                firstSeq++;
            }

            foreach (var loadedEvent in store.Load(aggregateRootId, firstSeq))
            {
                yield return loadedEvent;
                cacheForRoot.TryAdd(loadedEvent.GetSequenceNumber(), loadedEvent);
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            return store.Stream(globalSequenceNumber);
        }

        public long GetNextGlobalSequenceNumber()
        {
            return store.GetNextGlobalSequenceNumber();
        }
    }
}