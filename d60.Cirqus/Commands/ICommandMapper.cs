using System;

namespace d60.Cirqus.Commands
{
    /// <summary>
    /// Service that is responsible for providing some code to execute when given an instance of a command
    /// </summary>
    public interface ICommandMapper
    {
        /// <summary>
        /// Gets an action to execute for the given command
        /// </summary>
        Action<ICommandContext, Command> GetCommandAction(Command command);
    }
}