using System;
using d60.Cirqus.Commands;

namespace d60.Cirqus
{
    /// <summary>
    /// Command processor API - basically just processes commands :)
    /// </summary>
    public interface ICommandProcessor : IDisposable
    {
        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        CommandProcessingResult ProcessCommand(Command command);
    }
}