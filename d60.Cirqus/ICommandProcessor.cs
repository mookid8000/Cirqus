using d60.Cirqus.Commands;

namespace d60.Cirqus
{
    public interface ICommandProcessor
    {
        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        void ProcessCommand(Command command);
    }
}