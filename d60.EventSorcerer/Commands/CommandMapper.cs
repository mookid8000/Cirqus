using System;
using System.Collections.Concurrent;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.Commands
{
    public class CommandMapper : ICommandMapper
    {
        readonly ConcurrentDictionary<Type, Delegate> _mappings = new ConcurrentDictionary<Type, Delegate>();

        public CommandMapper Map<TCommand, TAggregate>(Action<TCommand, TAggregate> action)
            where TAggregate : AggregateRoot
            where TCommand : Command<TAggregate>
        {
            _mappings[typeof (TCommand)] = action;

            return this;
        }

        public Action<TCommand, TAggregateRoot> GetHandlerFor<TCommand, TAggregateRoot>()
            where TCommand : Command<TAggregateRoot>
            where TAggregateRoot : AggregateRoot
        {
            Delegate action;
            var commandType = typeof(TCommand);

            if (!_mappings.TryGetValue(commandType, out action))
            {
                throw new ArgumentException(string.Format("Could not map command of type {0} to a handler!", commandType));
            }

            try
            {
                return (Action<TCommand, TAggregateRoot>) action;
            }
            catch (Exception exception)
            {
                throw new ArgumentException(
                    string.Format("Found a handler for command {0} but it didn't match the aggregate type {1}",
                        commandType, typeof (TAggregateRoot)), exception);
            }
        }
    }
}