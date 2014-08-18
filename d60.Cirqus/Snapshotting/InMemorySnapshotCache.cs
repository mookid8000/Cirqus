using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace d60.Cirqus.Snapshotting
{
    public class InMemorySnapshotCache : ISnapshotCache
    {
        static Logger _logger;

        static InMemorySnapshotCache()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        /// <summary>
        /// Deep-cloning contract resolver for JSON.NET
        /// </summary>
        class JohnnyDeep : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                .Select(p => base.CreateProperty(p, memberSerialization))
                            .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                       .Select(f => base.CreateProperty(f, memberSerialization)))
                            .ToList();
                props.ForEach(p => { p.Writable = true; p.Readable = true; });
                return props;
            }
        }

        static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new JohnnyDeep(),
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        readonly ConcurrentDictionary<Guid, ConcurrentDictionary<long, CacheEntry>> _cacheEntries = new ConcurrentDictionary<Guid, ConcurrentDictionary<long, CacheEntry>>();

        class CacheEntry
        {
            CacheEntry()
            {
            }

            public static CacheEntry Create<TAggregateRoot>(AggregateRootInfo<TAggregateRoot> aggregateRootInfo)
                where TAggregateRoot : AggregateRoot
            {
                var rootInstance = aggregateRootInfo.AggregateRoot;
                var sequenceNumberGenerator = rootInstance.SequenceNumberGenerator;
                var aggregateRootRepository = rootInstance.AggregateRootRepository;
                var unitOfWork = rootInstance.UnitOfWork;
                try
                {

                    rootInstance.AggregateRootRepository = null;
                    rootInstance.SequenceNumberGenerator = null;
                    rootInstance.UnitOfWork = null;

                    var data = JsonConvert.SerializeObject(rootInstance, SerializerSettings);

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
                    rootInstance.SequenceNumberGenerator = sequenceNumberGenerator;
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
                var instance = JsonConvert.DeserializeObject<TAggregateRoot>(Data, SerializerSettings);

                return AggregateRootInfo<TAggregateRoot>.Old(instance, SequenceNumber, GlobalSequenceNumber);
            }
        }

        public AggregateRootInfo<TAggregateRoot> GetCloneFromCache<TAggregateRoot>(Guid aggregateRootId, long globalSequenceNumber) where TAggregateRoot : AggregateRoot, new()
        {
            var entriesForThisRoot = _cacheEntries.GetOrAdd(aggregateRootId, id => new ConcurrentDictionary<long, CacheEntry>());

            CacheEntry entry;

            if (!entriesForThisRoot.TryGetValue(globalSequenceNumber, out entry))
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