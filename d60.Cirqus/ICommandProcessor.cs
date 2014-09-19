using System;
using d60.Cirqus.Commands;

namespace d60.Cirqus
{
    public interface ICommandProcessor : IDisposable
    {
        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        CommandProcessingResult ProcessCommand(Command command);
    }

    public class CommandProcessingResult
    {
        readonly long? _newPosition;

        protected CommandProcessingResult(long? newPosition)
        {
            _newPosition = newPosition;
        }

        public static CommandProcessingResult NoEvents()
        {
            return new CommandProcessingResult(null);
        }

        public static CommandProcessingResult WithNewPosition(long newPosition)
        {
            return new CommandProcessingResult(newPosition);
        }

        public bool EventsWereEmitted
        {
            get { return _newPosition.HasValue; }
        }

        public long GetNewPosition()
        {
            if (!_newPosition.HasValue)
            {
                throw new InvalidOperationException("Cannot get new position from a command processing result when no events were emitted!");
            }

            return _newPosition.Value;
        }

        public override string ToString()
        {
            return EventsWereEmitted 
                ? string.Format("New position: {0}", _newPosition) 
                : "No events were emitted";
        }
    }
}