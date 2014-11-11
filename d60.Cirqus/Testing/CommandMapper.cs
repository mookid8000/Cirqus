using System;
using d60.Cirqus.Commands;

namespace d60.Cirqus.Testing
{
    public class CommandMapper
    {
        public Action<ICommandContext, Command> GetHandleFor(Command command)
        {
            if (command is ExecutableCommand)
            {
                return (context, executableCommand) => ((ExecutableCommand)executableCommand).Execute(context);
            }

            throw new ArgumentException(string.Format(@"Could not find a command mapping for the command {0}", command));
        }
    }
}