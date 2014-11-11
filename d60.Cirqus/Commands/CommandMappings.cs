using System;
using System.Collections.Generic;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Represents a group of command mappings, each mapping a specific command type to an action to be executed for that command.
    /// </summary>
    public class CommandMappings
    {
        readonly Dictionary<Type, Action<ICommandContext, Command>> _commandActions = new Dictionary<Type, Action<ICommandContext, Command>>();

        /// <summary>
        /// Adds a mapping for the specified command, allowing you to map a specific command to some actual method calls on aggregate roots
        /// </summary>
        public CommandMappings Map<TCommand>(Action<ICommandContext, TCommand> commandAction) where TCommand : Command
        {
            var commandType = typeof(TCommand);
            
            if (_commandActions.ContainsKey(commandType))
            {
                throw new InvalidOperationException(string.Format("Cannot add a command mapping for {0} because one has already been added!", commandType));
            }
            
            _commandActions[commandType] = (context, command) => commandAction(context, (TCommand)command);
            
            return this;
        }

        internal ICommandMapper CreateCommandMapperDecorator(ICommandMapper innerCommandMapper)
        {
            return new CommandMapperDecorator(innerCommandMapper, this);
        }

        internal Action<ICommandContext, Command> GetHandlerFor(Command command)
        {
            Action<ICommandContext, Command> action;

            return _commandActions.TryGetValue(command.GetType(), out action)
                ? action
                : null;
        }

        class CommandMapperDecorator : ICommandMapper
        {
            readonly ICommandMapper _innerCommandMapper;
            readonly CommandMappings _commandMappings;

            public CommandMapperDecorator(ICommandMapper innerCommandMapper, CommandMappings commandMappings)
            {
                _innerCommandMapper = innerCommandMapper;
                _commandMappings = commandMappings;
            }

            public Action<ICommandContext, Command> GetCommandAction(Command command)
            {
                return _commandMappings.GetHandlerFor(command)
                       ?? _innerCommandMapper.GetCommandAction(command);
            }
        }
    }
}