using System;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Default implementation of <see cref="ICommandMapper"/> that automatically executes commands based on <see cref="ExecutableCommand"/>
    /// </summary>
    public class DefaultCommandMapper : ICommandMapper
    {
        public Action<ICommandContext, Command> GetCommandAction(Command command)
        {
            if (command is ExecutableCommand)
            {
                return (context, executableCommand) => ((ExecutableCommand)executableCommand).Execute(context);
            }

            throw new ArgumentException(string.Format(@"Could not find a command mapping for the command {0} - please derive your command off of ExecutableCommand or the generic Command<TAggregateRoot>, or supply an action mapping when configuring the command processor by going .Options(o => o.AddCommandMappings(...))", command));
        }
    }
}