using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Commands
{
    public abstract class Command
    {
    }
    
    public abstract class Command<TAggregateRoot> : Command where TAggregateRoot : AggregateRoot
    {
        protected Command(Guid aggregateRootId)
        {
            AggregateRootId = aggregateRootId;
            Meta = new Metadata();
        }

        public Metadata Meta { get; private set; }
        public Guid AggregateRootId { get; private set; }
    }
}