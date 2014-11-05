using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    public interface ICommandContext
    {
        TAggregateRoot Load<TAggregateRoot>(string aggregateRootId, bool createIfNotExists = false) where TAggregateRoot : AggregateRoot, new();
    }
}