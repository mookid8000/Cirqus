using System;
using System.Linq;
using System.Reflection;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;
using d60.EventSorcerer.Views;

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
        readonly IEventStore _eventStore;
        readonly ICommandMapper _commandMapper;
        readonly ISequenceNumberGenerator _sequenceNumberGenerator;
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventDispatcher _eventDispatcher;

        public EventSorcererConfig(IEventStore eventStore, IAggregateRootRepository aggregateRootRepository, ICommandMapper commandMapper, ISequenceNumberGenerator sequenceNumberGenerator, IEventDispatcher eventDispatcher)
        {
            if (CommandProcessorMethod == null)
            {
                throw new ApplicationException(string.Format("Could not find the expected eventDispatcher method '{0}' on {1}", InnerProcessMethodName, GetType()));
            }

            _eventStore = eventStore;
            _aggregateRootRepository = aggregateRootRepository;
            _commandMapper = commandMapper;
            _sequenceNumberGenerator = sequenceNumberGenerator;
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
                var errorMessage = string.Format("Could not process command {0} when attempting to dispatch dynamically to ProcessCommand<{1}, {2}>(command)",
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

                Retryer.RetryOn<ConcurrencyException>(
                    () => DoProcessCommand<TAggregateRoot, TCommand>(batchId, command));
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
            aggregateRoot.SequenceNumberGenerator = new CachingSequenceNumberGenerator(_sequenceNumberGenerator);

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
            _eventDispatcher.Dispatch(emittedEvents);
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
                baseCommandType = commandType.BaseType;
            } while (baseCommandType != null);

            throw new ArgumentException(string.Format("Could not find Command<> base type from which {0} should have been derived", commandType));
        }
    }
}