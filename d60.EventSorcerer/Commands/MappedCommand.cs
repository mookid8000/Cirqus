using System;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Commands
{
    /// <summary>
    /// Command base class that allows for defining the command mapping in the derived command
    /// </summary>
    /// <typeparam name="TAggregateRoot">Specifies the type of aggregate root that this command targets</typeparam>
    public abstract class MappedCommand<TAggregateRoot> : Command<TAggregateRoot> where TAggregateRoot : AggregateRoot
    {
        protected MappedCommand(Guid aggregateRootId) : base(aggregateRootId)
        {
        }

        public abstract void Execute(TAggregateRoot aggregateRoot);
    }
}