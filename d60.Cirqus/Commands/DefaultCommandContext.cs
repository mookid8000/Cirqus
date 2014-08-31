using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    class DefaultCommandContext : ICommandContext
    {
        readonly RealUnitOfWork _unitOfWork;
        readonly IAggregateRootRepository _aggregateRootRepository;

        public DefaultCommandContext(RealUnitOfWork unitOfWork, IAggregateRootRepository aggregateRootRepository)
        {
            _unitOfWork = unitOfWork;
            _aggregateRootRepository = aggregateRootRepository;
        }

        public TAggregateRoot Load<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var cachedAggregateRoot = _unitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, long.MaxValue);

            if (cachedAggregateRoot != null)
            {
                return cachedAggregateRoot;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, unitOfWork: _unitOfWork);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            _unitOfWork.AddToCache(aggregateRoot, long.MaxValue);

            if (aggregateRootInfo.IsNew)
            {
                aggregateRoot.InvokeCreated();
            }

            return aggregateRootInfo.AggregateRoot;
        }
    }
}