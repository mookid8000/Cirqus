using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Testing
{
    /// <summary>
    /// Use the test context to carry out realistic testing of command processing, aggregate roots, and 
    /// event processing in view generation.
    /// </summary>
    public class TestContext : IDisposable
    {
        readonly IDomainEventSerializer _domainEventSerializer;
        readonly IDomainTypeNameMapper _domainTypeNameMapper = new DefaultDomainTypeNameMapper();
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly ViewManagerEventDispatcher _viewManagerEventDispatcher;
        readonly IEventDispatcher _eventDispatcher;
        readonly ViewManagerWaitHandle _waitHandle = new ViewManagerWaitHandle();
        readonly List<IViewManager> _addedViews = new List<IViewManager>();
        readonly ICommandMapper _testCommandMapper = new TestCommandMapper();
        readonly InMemoryEventStore _eventStore;

        DateTime _currentTime = DateTime.MinValue;
        bool _initialized;

        internal TestContext(InMemoryEventStore eventStore, IAggregateRootRepository aggregateRootRepository, IEventDispatcher eventDispatcher,
            IDomainEventSerializer domainEventSerializer, ICommandMapper commandMapper, IDomainTypeNameMapper domainTypeNameMapper)
        {
            _eventStore = eventStore;
            _aggregateRootRepository = aggregateRootRepository;
            _eventDispatcher = eventDispatcher;
            _domainEventSerializer = domainEventSerializer;
            _testCommandMapper = commandMapper;
            _domainTypeNameMapper = domainTypeNameMapper;

            _viewManagerEventDispatcher = eventDispatcher as ViewManagerEventDispatcher;
            if (_viewManagerEventDispatcher != null)
            {
                _waitHandle.Register(_viewManagerEventDispatcher);
            }
        }

        public static IOptionalConfiguration<TestContext> With()
        {
            return new TestContextConfigurationBuilder();
        }

        public static TestContext Create()
        {
            return With().Create();
        }

        internal event Action Disposed = delegate { };
        internal bool Asynchronous { get; set; }

        public TestContext AddViewManager(IViewManager viewManager)
        {
            if (_viewManagerEventDispatcher == null) return this;

            _addedViews.Add(viewManager);
            _viewManagerEventDispatcher.AddViewManager(viewManager);

            return this;
        }

        /// <summary>
        /// Processes the specified command in a unit of work.
        /// </summary>
        public CommandProcessingResultWithEvents ProcessCommand(Command command)
        {
            using (var unitOfWork = BeginUnitOfWork())
            {
                var handler = _testCommandMapper.GetCommandAction(command);

                handler(new DefaultCommandContext(unitOfWork.RealUnitOfWork), command);

                var eventsToReturn = unitOfWork.EmittedEvents.ToList();

                foreach (var e in eventsToReturn)
                {
                    e.Meta.Merge(command.Meta);
                }

                unitOfWork.Commit();

                var result = new CommandProcessingResultWithEvents(eventsToReturn);

                if (!Asynchronous)
                {
                    WaitForViewsToCatchUp();
                }

                return result;
            }
        }

        /// <summary>
        /// Creates a new unit of work similar to the one within which a command is processed.
        /// </summary>
        public TestUnitOfWork BeginUnitOfWork()
        {
            var unitOfWork = new TestUnitOfWork(_aggregateRootRepository, _eventStore, _eventDispatcher, _domainEventSerializer, _domainTypeNameMapper);

            unitOfWork.Committed += () =>
            {
                if (!Asynchronous)
                {
                    WaitForViewsToCatchUp();
                }
            };

            return unitOfWork;
        }

        /// <summary>
        /// Fixes the current time to the specified <see cref="fixedCurrentTime"/> which will cause emitted events to have that
        /// time as their <see cref="DomainEvent.MetadataKeys.TimeUtc"/> header
        /// </summary>
        public void SetCurrentTime(DateTime fixedCurrentTime)
        {
            _currentTime = fixedCurrentTime;
        }

        /// <summary>
        /// Gets the entire history of commited events from the event store
        /// </summary>
        public EventCollection History
        {
            get { return new EventCollection(_eventStore.Stream().Select(e => _domainEventSerializer.Deserialize(e))); }
        }

        /// <summary>
        /// Hydrates and returns all aggregate roots from the entire history of the test context
        /// </summary>
        public IEnumerable<AggregateRoot> AggregateRoots
        {
            get
            {
                return _eventStore
                    .Select(e => e.GetAggregateRootId()).Distinct()
                    .Select(aggregateRootId =>
                    {
                        var firstEvent = _eventStore.Load(aggregateRootId).First();
                        var typeName = firstEvent.Meta[DomainEvent.MetadataKeys.Owner] ?? "";
                        var type = TryGetType(typeName);

                        if (type == null) return null;

                        var unitOfWork = new RealUnitOfWork(_aggregateRootRepository, _domainTypeNameMapper);

                        var parameters = new object[]
                        {
                            aggregateRootId, 
                            unitOfWork, 
                            long.MaxValue,      // max global sequence number
                            false               // createIfNotExists
                        };

                        try
                        {
                            var aggregateRoot = _aggregateRootRepository
                                .GetType()
                                .GetMethod("Get")
                                .MakeGenericMethod(type)
                                .Invoke(_aggregateRootRepository, parameters);

                            return (AggregateRoot)aggregateRoot;
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException(string.Format("Could not hydrate aggregate root {0} with ID {1}!", type, aggregateRootId), exception);
                        }
                    })
                    .Where(aggregateRoot => aggregateRoot != null);
            }
        }

        Type TryGetType(string typeName)
        {
            try
            {
                return _domainTypeNameMapper.GetType(typeName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Saves the given domain event to the history - requires that the aggregate root ID has been added in the event's metadata under the <see cref="DomainEvent.MetadataKeys.AggregateRootId"/> key
        /// </summary>
        public CommandProcessingResultWithEvents Save<TAggregateRoot>(DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            if (!domainEvent.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Canno save domain event {0} because it does not have an aggregate root ID! Use the Save(id, event) overload or make sure that the '{1}' metadata key has been set",
                        domainEvent, DomainEvent.MetadataKeys.AggregateRootId));
            }

            return Save(domainEvent.GetAggregateRootId(), domainEvent);
        }

        /// <summary>
        /// Saves the given domain event to the history as if it was emitted by the specified aggregate root, immediately dispatching the event to the event dispatcher
        /// </summary>
        public CommandProcessingResultWithEvents Save<TAggregateRoot>(string aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            return Save(aggregateRootId, new[] { domainEvent });
        }

        /// <summary>
        /// Saves the given domain events to the history as if they were emitted by the specified aggregate root, immediately dispatching the events to the event dispatcher
        /// </summary>
        public CommandProcessingResultWithEvents Save<TAggregateRoot>(string aggregateRootId, params DomainEvent<TAggregateRoot>[] domainEvents) where TAggregateRoot : AggregateRoot
        {
            foreach (var domainEvent in domainEvents)
            {
                SetMetadata(aggregateRootId, domainEvent);
            }

            var eventData = domainEvents.Select(e => _domainEventSerializer.Serialize(e)).ToList();

            _eventStore.Save(Guid.NewGuid(), eventData);

            _eventDispatcher.Dispatch(_eventStore, domainEvents);

            var result = new CommandProcessingResultWithEvents(domainEvents);

            if (!Asynchronous)
            {
                WaitForViewsToCatchUp();
            }

            return result;
        }

        /// <summary>
        /// Waits for all views to catch up with the entire history of events, timing out if that takes longer than 10 seconds
        /// </summary>
        public void WaitForViewsToCatchUp(int timeoutSeconds = 10)
        {
            var allGlobalSequenceNumbers = History.Select(h => h.GetGlobalSequenceNumber()).ToArray();

            if (!allGlobalSequenceNumbers.Any()) return;

            var result = CommandProcessingResult.WithNewPosition(allGlobalSequenceNumbers.Max());

            try
            {
                _waitHandle.WaitForAll(result, TimeSpan.FromSeconds(timeoutSeconds)).Wait();
            }
            catch (TimeoutException exception)
            {
                throw new TimeoutException(string.Format(@"One or more views did not catch up within {0} s timeout

Current view positions:
{1}", timeoutSeconds, string.Join(Environment.NewLine, _addedViews.Select(viewManager => string.Format("    {0}: {1}", viewManager.GetPosition().ToString().PadRight(5), viewManager.GetType().FullName)))), exception);
            }
        }

        /// <summary>
        /// Waits for views managing the specified <see cref="TViewInstance"/> to catch up with the entire history of events, timing out if that takes longer than 10 seconds
        /// </summary>
        public void WaitForViewToCatchUp<TViewInstance>(int timeoutSeconds = 10) where TViewInstance : IViewInstance
        {
            var allGlobalSequenceNumbers = History.Select(h => h.GetGlobalSequenceNumber()).ToArray();

            if (!allGlobalSequenceNumbers.Any()) return;

            var result = CommandProcessingResult.WithNewPosition(allGlobalSequenceNumbers.Max());

            _waitHandle.WaitFor<TViewInstance>(result, TimeSpan.FromSeconds(timeoutSeconds)).Wait();
        }

        public void Initialize()
        {
            if (!_initialized)
            {
                if (_viewManagerEventDispatcher != null)
                {
                    _viewManagerEventDispatcher.Initialize(_eventStore, purgeExistingViews: true);
                }

                _initialized = true;
            }
        }

        void SetMetadata<TAggregateRoot>(string aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            var now = GetNow();

            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _eventStore.GetNextSeqNo(aggregateRootId).ToString(Metadata.NumberCulture);
            domainEvent.Meta[DomainEvent.MetadataKeys.Owner] = _domainTypeNameMapper.GetName(typeof(TAggregateRoot));
            domainEvent.Meta[DomainEvent.MetadataKeys.Type] = _domainTypeNameMapper.GetName(domainEvent.GetType());
            domainEvent.Meta[DomainEvent.MetadataKeys.TimeUtc] = now.ToString("u");

            domainEvent.Meta.TakeFromAttributes(domainEvent.GetType());
            domainEvent.Meta.TakeFromAttributes(typeof(TAggregateRoot));

            EnsureSerializability(domainEvent);
        }

        void EnsureSerializability(DomainEvent domainEvent)
        {
            var firstSerialization = _domainEventSerializer.Serialize(domainEvent);

            var secondSerialization = _domainEventSerializer.Serialize(
                _domainEventSerializer.Deserialize(firstSerialization));

            if (firstSerialization.IsSameAs(secondSerialization)) return;

            throw new ArgumentException(string.Format(@"Could not properly roundtrip the following domain event: {0}

Result after first serialization:

{1}

Result after roundtripping:

{2}

Headers: {3}", domainEvent, firstSerialization, secondSerialization, domainEvent.Meta));
        }

        DateTime GetNow()
        {
            if (_currentTime == DateTime.MinValue)
            {
                return DateTime.UtcNow;
            }

            var timeToReturn = _currentTime.ToUniversalTime();

            // simulate time progressing
            _currentTime = _currentTime.AddTicks(1);

            return timeToReturn;
        }

        bool _disposed;

        ~TestContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_viewManagerEventDispatcher != null)
                {
                    _viewManagerEventDispatcher.Dispose();
                }

                Disposed();
            }

            _disposed = true;
        }
    }
}