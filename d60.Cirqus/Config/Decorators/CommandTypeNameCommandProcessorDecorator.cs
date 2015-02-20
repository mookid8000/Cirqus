using d60.Cirqus.Commands;
using d60.Cirqus.Events;

namespace d60.Cirqus.Config.Decorators
{
    /// <summary>
    /// Decorator of <see cref="ICommandProcessor"/> that automatically adds the command type name to the metadata of commands
    /// </summary>
    class CommandTypeNameCommandProcessorDecorator : ICommandProcessor
    {
        readonly ICommandProcessor _innerCommandProcessor;
        readonly IDomainTypeNameMapper _domainTypeNameMapper;

        public CommandTypeNameCommandProcessorDecorator(ICommandProcessor innerCommandProcessor, IDomainTypeNameMapper domainTypeNameMapper)
        {
            _innerCommandProcessor = innerCommandProcessor;
            _domainTypeNameMapper = domainTypeNameMapper;
        }

        public CommandProcessingResult ProcessCommand(Command command)
        {
            if (!command.Meta.ContainsKey(DomainEvent.MetadataKeys.CommandTypeName))
            {
                var commandTypeName = _domainTypeNameMapper.GetName(command.GetType());
                command.Meta[DomainEvent.MetadataKeys.CommandTypeName] = commandTypeName;
            }

            return _innerCommandProcessor.ProcessCommand(command);
        }

        public void Dispose()
        {
            _innerCommandProcessor.Dispose();
        }
    }
}