using System;
using System.Collections.Concurrent;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Snapshotting
{
    public class InMemorySnapshotCache : ISnapshotCache
    {
        static Logger _logger;

        static InMemorySnapshotCache()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly ConcurrentDictionary<Guid, ConcurrentDictionary<long, CacheEntry>> _cacheEntries = new ConcurrentDictionary<Guid, ConcurrentDictionary<long, CacheEntry>>();

        internal class CacheEntry
        {
            static Sturdylizer _sturdylizer = new Sturdylizer();
            CacheEntry()
            {
            }

            public static CacheEntry Create<TAggregateRoot>(AggregateRootInfo<TAggregateRoot> aggregateRootInfo)
                where TAggregateRoot : AggregateRoot
            {
                var rootInstance = aggregateRootInfo.AggregateRoot;
                var aggregateRootRepository = rootInstance.AggregateRootRepository;
                var unitOfWork = rootInstance.UnitOfWork;
                try
                {

                    rootInstance.AggregateRootRepository = null;
                    rootInstance.UnitOfWork = null;

                    var data = _sturdylizer.SerializeObject(rootInstance);

                    return new CacheEntry
                    {
                        SequenceNumber = aggregateRootInfo.LastSeqNo,
                        GlobalSequenceNumber = aggregateRootInfo.LastGlobalSeqNo,
                        Hits = 0,
                        TimeOfLastHit = DateTime.UtcNow,
                        Data = data
                    };
                }
                finally
                {
                    rootInstance.AggregateRootRepository = aggregateRootRepository;
                    rootInstance.UnitOfWork = unitOfWork;
                }
            }

            public string Data { get; set; }

            public long SequenceNumber { get; private set; }

            public long GlobalSequenceNumber { get; private set; }

            public int Hits { get; private set; }

            public DateTime TimeOfLastHit { get; private set; }

            public void IncrementHits()
            {
                Hits++;
                TimeOfLastHit = DateTime.UtcNow;
            }

            public AggregateRootInfo<TAggregateRoot> GetCloneAs<TAggregateRoot>() where TAggregateRoot : AggregateRoot
            {
                var instance = (TAggregateRoot)_sturdylizer.DeserializeObject(Data);

                return AggregateRootInfo<TAggregateRoot>.Old(instance, SequenceNumber, GlobalSequenceNumber);
            }
        }

        public AggregateRootInfo<TAggregateRoot> GetCloneFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            var entriesForThisRoot = _cacheEntries.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());

            CacheEntry entry;

            var availableSequenceNumbersForThisRoot = entriesForThisRoot
                .Where(e => e.Value.GlobalSequenceNumber <= globalSequenceNumber)
                .Select(e => e.Value.GlobalSequenceNumber)
                .ToArray();

            if (!availableSequenceNumbersForThisRoot.Any()) return null;

            var highestSequenceNumberAvailableForThisRoot = availableSequenceNumbersForThisRoot.Max();
            if (!entriesForThisRoot.TryGetValue(highestSequenceNumberAvailableForThisRoot, out entry))
                return null;

            var aggregateRootInfoToReturn = entry.GetCloneAs<TAggregateRoot>();

            entry.IncrementHits();

            return aggregateRootInfoToReturn;
        }

        public void PutCloneToCache<TAggregateRoot>(AggregateRootInfo<TAggregateRoot> aggregateRootInfo) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootId = aggregateRootInfo.AggregateRootId;
            var entriesForThisRoot = _cacheEntries.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());

            entriesForThisRoot.TryAdd(aggregateRootInfo.LastGlobalSeqNo, CacheEntry.Create(aggregateRootInfo));
        }
    }
}