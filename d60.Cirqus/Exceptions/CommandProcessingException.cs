using System;
using System.Runtime.Serialization;
using d60.Cirqus.Commands;

namespace d60.Cirqus.Exceptions
{
    [Serializable]
    public class CommandProcessingException : ApplicationException
    {
        public CommandProcessingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public Command FailedCommand { get; private set; }

        CommandProcessingException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public static CommandProcessingException Create(Command command, Exception caughtException)
        {
            var message = string.Format("An error occurred while processing command {0} - any events emitted will most likely not have been saved", command);

            return new CommandProcessingException(message, caughtException)
            {
                FailedCommand = command
            };
        }
    }
}