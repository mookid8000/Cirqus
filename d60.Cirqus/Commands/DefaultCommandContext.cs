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

        public TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot
        {
            if (_unitOfWork.Exists(aggregateRootId, long.MaxValue))
            {
                throw new InvalidOperationException(string.Format("Cannot create aggregate root {0} with ID {1} because an instance with that ID already exists!",
                    typeof(TAggregateRoot), aggregateRootId));
            }

            return (TAggregateRoot)_unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true);
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            try
            {
                return _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false) as TAggregateRoot;
            }
            catch
            {
                return null;
            }
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            var aggregateRoot = _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false);

            return aggregateRoot as TAggregateRoot;
        }
    }
}