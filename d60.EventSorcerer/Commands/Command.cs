using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Commands
{
    /// <summary>
    /// Ultimate command base class - don't derive off of this one directly, use either <see cref="Command{TAggregateRoot}"/> or <see cref="MappedCommand{TAggregateRoot}"/>
    /// </summary>
    public abstract class Command
    {
    }

    /// <summary>
    /// Command base class that works with an externally defined command mapping. Requires that the command mapper is configured to map the derived command type to an operation on an aggregate root
    /// </summary>
    /// <typeparam name="TAggregateRoot">Specifies the type of aggregate root that this command targets</typeparam>
    // ReSharper disable UnusedTypeParameter
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
    // ReSharper restore UnusedTypeParameter
}