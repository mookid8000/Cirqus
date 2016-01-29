using System;
using System.Collections.Concurrent;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;

namespace d60.Cirqus.Snapshotting
{
    class NewSimpleSnapshottingAggregateRootRepositoryDecorator : IAggregateRootRepository
    {
        readonly ConcurrentDictionary<string, AggregateRootInfo> _cache = new ConcurrentDictionary<string, AggregateRootInfo>();
        readonly IAggregateRootRepository _aggregateRootRepository;

        public NewSimpleSnapshottingAggregateRootRepositoryDecorator(IAggregateRootRepository aggregateRootRepository)
        {
            _aggregateRootRepository = aggregateRootRepository;
        }

        public AggregateRoot Get<TAggregateRoot>(string aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue, bool createIfNotExists = false)
        {
            if (maxGlobalSequenceNumber < long.MaxValue)
            {
                var aggregateRoot = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);

                return aggregateRoot;
            }

            AggregateRootInfo info;

            if (_cache.TryRemove(aggregateRootId, out info))
            {
                //Console.WriteLine("HIT! {0}", aggregateRootId);

                unitOfWork.Committed += e =>
                {
                    //Console.WriteLine("put {0}", aggregateRootId);
                    _cache.TryAdd(aggregateRootId, info);
                };

                return info.Instance;
            }

            var instance = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork, maxGlobalSequenceNumber, createIfNotExists);

            unitOfWork.Committed += e =>
            {
                //Console.WriteLine("put {0}", aggregateRootId);
                _cache.TryAdd(aggregateRootId, new AggregateRootInfo(instance));
            };

            return instance;
        }

        public bool Exists(string aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue)
        {
            return _aggregateRootRepository.Exists(aggregateRootId, maxGlobalSequenceNumber);
        }
    }
}