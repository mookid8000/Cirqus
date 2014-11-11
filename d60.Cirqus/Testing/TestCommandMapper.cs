using System;
using System.Collections.Generic;
using d60.Cirqus.Commands;

namespace d60.Cirqus.Testing
{
    public class TestCommandMapper : ICommandMapper
    {
        readonly List<CommandMappings> _commandMappings = new List<CommandMappings>(); 
        readonly DefaultCommandMapper _defaultCommandMapper = new DefaultCommandMapper();

        public Action<ICommandContext, Command> GetCommandAction(Command command)
        {
            try
            {
                foreach (var mappings in _commandMappings)
                {
                    var handler = mappings.GetHandlerFor(command);

                    if (handler != null) return handler;
                }

                return _defaultCommandMapper.GetCommandAction(command);
            }
            catch (Exception exception)
            {
                throw new ArgumentException(string.Format("Could not find command action to execute for command {0} - please add suitable command mappings on the TestContext like this: context.AddCommandMappings(...)", command), exception);
            }
        }

        public void AddMappings(CommandMappings commandMappings)
        {
            _commandMappings.Add(commandMappings);
        }
    }
}