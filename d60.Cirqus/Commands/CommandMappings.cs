using System;
using System.Collections.Generic;

namespace d60.Cirqus.Commands
{
    public class CommandMappings
    {
        readonly Dictionary<Type, Action<ICommandContext, Command>> _commandActions = new Dictionary<Type, Action<ICommandContext, Command>>();

        public CommandMappings Map<TCommand>(Action<ICommandContext, TCommand> commandAction) where TCommand : Command
        {
            _commandActions[typeof(TCommand)] = (context, command) => commandAction(context, (TCommand) command);
            return this;
        }

        public ICommandMapper CreateCommandMapperDecorator(ICommandMapper innerCommandMapper)
        {
            return new CommandMapperDecorator(innerCommandMapper, this);
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

        internal Action<ICommandContext, Command> GetHandlerFor(Command command)
        {
            Action<ICommandContext, Command> action;

            return _commandActions.TryGetValue(command.GetType(), out action)
                ? action
                : null;
        }
    }
}