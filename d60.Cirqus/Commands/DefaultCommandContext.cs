using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Commands
{
    class DefaultCommandContext : ICommandContext
    {
        readonly RealUnitOfWork _unitOfWork;
        readonly Metadata _metadata;

        public DefaultCommandContext(RealUnitOfWork unitOfWork, Metadata metadata)
        {
            _unitOfWork = unitOfWork;
            _metadata = metadata;
        }


        public TAggregateRoot Create<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot
        {
            if (_unitOfWork.Exists(aggregateRootId, long.MaxValue))
            {
                throw new InvalidOperationException(string.Format("Cannot create aggregate root {0} with ID {1} because an instance with that ID already exists!",
                    typeof(TAggregateRoot), aggregateRootId));
            }

            var root = (TAggregateRoot)_unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true);

            root.CurrentCommandMetadata = _metadata;

            root.InvokeCreated();

            return root;
        }

        public TAggregateRoot TryLoad<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            try
            {
                var root = _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false);
                
                root.CurrentCommandMetadata = _metadata;

                return root as TAggregateRoot;
            }
            catch
            {
                return null;
            }
        }

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : class
        {
            var root = _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: false);
            
            root.CurrentCommandMetadata = _metadata;

            return root as TAggregateRoot;
        }
    }
}