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

        public TAggregateRoot Load<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootInfo = _unitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true);
            return aggregateRootInfo.AggregateRoot;
        }
    }
}