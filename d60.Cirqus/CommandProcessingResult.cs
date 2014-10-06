using System;

namespace d60.Cirqus
{
    /// <summary>
    /// Represents the result of processing a command in the form of the highest global sequence number that
    /// was applied to any events emitted during the processing. If no events were emitted, there will
    /// be no sequence number - check <see cref="EventsWereEmitted"/> before calling <see cref="GetNewPosition"/>
    /// </summary>
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