using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
{
    public abstract class CreateCommand<TAggregateRoot> :Command where TAggregateRoot : AggregateRoot, new()
    {
        protected CreateCommand(string aggregateRootId)
        {
            if (aggregateRootId == null)
                throw new ArgumentNullException("aggregateRootId", "You need to specify an aggregate root ID");

            AggregateRootId = aggregateRootId;
        }

        public string AggregateRootId { get; private set; }

        public override sealed void Execute(ICommandContext context)
        {
            var aggregateRootInstance = context.Load<TAggregateRoot>(AggregateRootId, createIfNotExists: true);

            Execute(aggregateRootInstance);
        }

        public abstract void Execute(TAggregateRoot aggregateRoot);

        public override string ToString()
        {
            return string.Format("{0} => {1} {2}", GetType().Name, typeof (TAggregateRoot).Name, AggregateRootId);
        }
    }
}