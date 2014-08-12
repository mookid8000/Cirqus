using System;
using System.Runtime.Serialization;
using d60.Circus.Aggregates;
using d60.Circus.Commands;

namespace d60.Circus.Exceptions
{
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

        public static CommandProcessingException Create<TAggregateRoot>(Command<TAggregateRoot> command, Exception caughtException)
            where TAggregateRoot : AggregateRoot
        {
            var message = string.Format("An error occurred while processing command {0} - any events emitted will most likely not have been saved", command);

            return new CommandProcessingException(message, caughtException)
            {
                FailedCommand = command
            };
        }
    }
}