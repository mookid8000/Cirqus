using d60.Cirqus.Commands;

namespace d60.Cirqus
{
    public interface ICommandProcessor
    {
        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        CommandProcessingResult ProcessCommand(Command command);
    }

    public class CommandProcessingResult
    {
        public CommandProcessingResult(long[] globalSequenceNumbersOfEmittedEvents)
        {
            GlobalSequenceNumbersOfEmittedEvents = globalSequenceNumbersOfEmittedEvents;
        }

        public long[] GlobalSequenceNumbersOfEmittedEvents { get; private set; }

        public bool EventsWereEmitted
        {
            get { return GlobalSequenceNumbersOfEmittedEvents.Length > 0; }
        }
    }
}