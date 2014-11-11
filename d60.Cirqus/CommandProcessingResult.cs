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

        /// <summary>
        /// Constructs a new <see cref="CommandProcessingResult"/> that indicates that no events were emitted
        /// </summary>
        public static CommandProcessingResult NoEvents()
        {
            return new CommandProcessingResult(null);
        }

        /// <summary>
        /// Constructs a new <see cref="CommandProcessingResult"/> that indicates that events were emitted, including the global
        /// sequence number of the last event
        /// </summary>
        public static CommandProcessingResult WithNewPosition(long newPosition)
        {
            return new CommandProcessingResult(newPosition);
        }

        /// <summary>
        /// Indicates whether events were emitted
        /// </summary>
        public bool EventsWereEmitted
        {
            get { return _newPosition.HasValue; }
        }

        /// <summary>
        /// Returns the global sequence number of the last event that was emitted. If no events were emitted, the number is unknown and
        /// this method will throw an exception
        /// </summary>
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
                ? string.Format("[NEW POSITION: {0}]", _newPosition) 
                : "[NEW POSITION: n/a]";
        }
    }
}