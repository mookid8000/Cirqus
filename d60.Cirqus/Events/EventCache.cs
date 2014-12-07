using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Events
{
    /// <summary>
    /// WARNING: Not fully functional. Do not use yet! :)
    /// </summary>
    public class EventCache : IEventStore 
    {
        readonly IEventStore _store;
        readonly int _maxEntries;
        readonly ConcurrentDictionary<string, ConcurrentQueue<EventData>> _cache; 

        public EventCache(IEventStore store, int maxEntries = 100000)
        {
            _store = store;
            _maxEntries = maxEntries;

            _cache = new ConcurrentDictionary<string, ConcurrentQueue<EventData>>();
        }

        public void Save(Guid batchId, IEnumerable<EventData> batch)
        {
            _store.Save(batchId, batch);
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            var cacheForRoot = _cache.GetOrAdd(aggregateRootId, id => new ConcurrentQueue<EventData>());

            foreach (var cachedEvent in cacheForRoot)
            {
                var sequenceNumberOfCachedEvent = cachedEvent.GetSequenceNumber();

                if (sequenceNumberOfCachedEvent < firstSeq)
                {
                    continue;
                }

                if (sequenceNumberOfCachedEvent > firstSeq)
                {
                    var missingFromCache = (int)(sequenceNumberOfCachedEvent - firstSeq);
                    
                    foreach (var loadedEvent in _store.Load(aggregateRootId, firstSeq).Take(missingFromCache))
                    {
                        yield return loadedEvent;
                        firstSeq++;
                    }
                }

                yield return cachedEvent;
                firstSeq++;
            }

            foreach (var loadedEvent in _store.Load(aggregateRootId, firstSeq))
            {
                yield return loadedEvent;
                cacheForRoot.Enqueue(loadedEvent);
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            return _store.Stream(globalSequenceNumber);
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _store.GetNextGlobalSequenceNumber();
        }
    }
}