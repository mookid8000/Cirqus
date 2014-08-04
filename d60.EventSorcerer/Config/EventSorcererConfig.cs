using System;
using System.Linq;
using System.Reflection;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;

namespace d60.EventSorcerer.Config
{
    /// <summary>
    /// Main event sorcerer thing - if you can successfully create this bad boy, you have a fully functioning event sourcing thing going for you
    /// </summary>
    public class EventSorcererConfig
    {
        const string InnerProcessMethodName = "InnerProcessCommand";

        static readonly MethodInfo CommandProcessorMethod =
            MethodBase.GetCurrentMethod().DeclaringType
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .SingleOrDefault(m => m.Name == InnerProcessMethodName && m.IsGenericMethod);

        readonly EventSorcererOptions _options = new EventSorcererOptions();
        readonly Retryer _retryer = new Retryer(10);
        readonly IEventStore _eventStore;
        readonly ICommandMapper _commandMapper;
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventDispatcher _eventDispatcher;

        public EventSorcererConfig(IEventStore eventStore, IAggregateRootRepository aggregateRootRepository, ICommandMapper commandMapper, IEventDispatcher eventDispatcher)
        {
            if (CommandProcessorMethod == null)
            {
                throw new ApplicationException(string.Format("Could not find the expected eventDispatcher method '{0}' on {1}", InnerProcessMethodName, GetType()));
            }

            _eventStore = eventStore;
            _aggregateRootRepository = aggregateRootRepository;
            _commandMapper = commandMapper;
            _eventDispatcher = eventDispatcher;
        }

        public EventSorcererOptions Options
        {
            get { return _options; }
        }

        /// <summary>
        /// Processes the specified command by invoking the generic eventDispatcher method
        /// </summary>
        public void ProcessCommand(Command command)
        {
            var commandType = command.GetType();
            var aggregateRootType = GetAggregateRootType(commandType);

            try
            {
                CommandProcessorMethod
                    .MakeGenericMethod(aggregateRootType, commandType)
                    .Invoke(this, new object[] { command });
            }
            catch (Exception exception)
            {
                var errorMessage = string.Format("Could not process command {0} when attempting to dispatch dynamically to InnerProcessCommand<{1}, {2}>(command)",
                    command, aggregateRootType.Name, commandType.Name);

                throw new ApplicationException(errorMessage, exception);
            }
        }

        // ReSharper disable UnusedMember.Local
        /// <summary>
        /// This method is called via reflection!
        /// </summary>
        void InnerProcessCommand<TAggregateRoot, TCommand>(TCommand command)
            where TCommand : Command<TAggregateRoot>
            where TAggregateRoot : AggregateRoot, new()
        {
            try
            {
                var batchId = Guid.NewGuid();

                _retryer.RetryOn<ConcurrencyException>(() => DoProcessCommand<TAggregateRoot, TCommand>(batchId, command));
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("An error occurred while processing command {0}", command), exception);
            }
        }
        // ReSharper restore UnusedMember.Local

        void DoProcessCommand<TAggregateRoot, TCommand>(Guid batchId, TCommand command)
            where TCommand : Command<TAggregateRoot>
            where TAggregateRoot : AggregateRoot, new()
        {
            var unitOfWork = new UnitOfWork();
            var handler = _commandMapper.GetHandlerFor<TCommand, TAggregateRoot>();
            var aggregateRoot = _aggregateRootRepository.Get<TAggregateRoot>(command.AggregateRootId);

            aggregateRoot.EventCollector = unitOfWork;
            aggregateRoot.SequenceNumberGenerator = new CachingSequenceNumberGenerator(_eventStore);

            handler(command, aggregateRoot);

            var emittedEvents = unitOfWork.EmittedEvents.ToList();

            if (!emittedEvents.Any()) return;

            foreach (var e in emittedEvents)
            {
                e.Meta.Merge(command.Meta);
            }

            // first: save the events
            _eventStore.Save(batchId, emittedEvents);

            // when we come to this place, we deliver the events to the view manager
            _eventDispatcher.Dispatch(_eventStore, emittedEvents);
        }

        static Type GetAggregateRootType(Type commandType)
        {
            var baseCommandType = commandType;

            do
            {
                if (baseCommandType.IsGenericType && baseCommandType.GetGenericTypeDefinition() == typeof(Command<>))
                {
                    return baseCommandType.GetGenericArguments().Single();
                }
                baseCommandType = baseCommandType.BaseType;
            } while (baseCommandType != null);

            throw new ArgumentException(string.Format("Could not find the generic Command<> base type from which {0} should have been derived - please derive commands off of the generic Command<> type, closing it with the type of the aggregate root that the command targets, e.g. Command<SomeAggregateRoot>", commandType));
        }
    }
}