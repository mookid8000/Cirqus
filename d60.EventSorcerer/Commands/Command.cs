using System;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Commands
{
    public class Command
    {
    }
    
    public abstract class Command<TAggregateRoot> : Command where TAggregateRoot : AggregateRoot
    {
        protected Command(Guid aggregateRootId)
        {
            AggregateRootId = aggregateRootId;
        }

        public Guid AggregateRootId { get; private set; }

        public Type AggregateRootType
        {
            get { return typeof (TAggregateRoot); }
        }
    }
}