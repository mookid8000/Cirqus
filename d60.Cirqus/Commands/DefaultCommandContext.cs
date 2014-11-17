using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    class DefaultCommandContext : ICommandContext
    {
        readonly RealUnitOfWork _unitOfWork;

        public DefaultCommandContext(RealUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            if (_unitOfWork.Exists<TAggregateRoot>(aggregateRootId, long.MaxValue))
            {
                throw new InvalidOperationException(string.Format("Cannot create aggregate root {0} with ID {1} because an instance with that ID already exists!",
                    typeof(TAggregateRoot), aggregateRootId));
            }

            return _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true).AggregateRoot;
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            try
            {
                return _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false).AggregateRoot;
            }
            catch
            {
                return null;
            }
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId)
        {
            throw new NotImplementedException("not yet");
            //var aggregateRootInfo = _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false);

            //return aggregateRootInfo.AggregateRoot;
        }
    }
}