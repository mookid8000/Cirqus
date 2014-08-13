using d60.Cirqus.Commands;
using d60.Cirqus.Config;

namespace d60.Cirqus
{
    public interface ICommandProcessor
    {
        /// <summary>
        /// Initializes the views, giving them a chance to catch up to the current state
        /// </summary>
        CommandProcessor Initialize();

        /// <summary>
        /// Gets the currently active options for this command processor
        /// </summary>
        Options Options { get; }

        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        void ProcessCommand(Command command);
    }
}