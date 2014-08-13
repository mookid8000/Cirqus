using System;
using System.Collections.Concurrent;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Commands
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
            if (typeof(Command<TAggregateRoot>).IsAssignableFrom(typeof(TCommand)))
            {
                return (command, root) =>
                {
                    // for some reason, an unconditional cast cannot be performed here - therefore:
                    var autoMapCommand = command as Command<TAggregateRoot>;

                    if (autoMapCommand == null)
                    {
                        throw new ApplicationException(string.Format("Could not transform {0} into MappedCommand<{1}> - this should be impossible though, so it's crazy that we've ended up here :(", command, typeof(TAggregateRoot)));
                    }

                    autoMapCommand.Execute(root);
                };
            }

            Delegate action;
            var commandType = typeof(TCommand);

            if (!_mappings.TryGetValue(commandType, out action))
            {
                throw new ArgumentException(string.Format("Could not map command of type {0} to a handler! In order to be able to process a command, the command must be mapped to one or more operations on an aggregate root - e.g. like so: commandMapper.Map<SomeCommand, SomeAggregateRoot>((command, root) => root.DoStuff(command.SomeParameter))", commandType));
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