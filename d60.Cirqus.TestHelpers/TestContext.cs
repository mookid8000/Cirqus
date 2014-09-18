using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;
using ViewManagerEventDispatcher = d60.Cirqus.Views.ViewManagerEventDispatcher;

namespace d60.Cirqus.TestHelpers
{
    /// <summary>
    /// Use this bad boy to test your CQRS+ES things
    /// </summary>
    public class TestContext : IDisposable
    {
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer("<events>");
        readonly InMemoryEventStore _eventStore = new InMemoryEventStore();
        readonly DefaultAggregateRootRepository _aggregateRootRepository;
        readonly Views.ViewManagers.Old.ViewManagerEventDispatcher _oldViewManagerEventDispatcher;
        readonly ViewManagerEventDispatcher _viewManagerEventDispatcher;
        readonly CompositeEventDispatcher _eventDispatcher;
        readonly ViewManagerWaitHandle _waitHandle = new ViewManagerWaitHandle();

        DateTime _currentTime = DateTime.MinValue;
        bool _initialized;

        public TestContext()
        {
            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);

            _oldViewManagerEventDispatcher = new Views.ViewManagers.Old.ViewManagerEventDispatcher(_aggregateRootRepository);
            _viewManagerEventDispatcher = new ViewManagerEventDispatcher(_aggregateRootRepository, _eventStore);

            _waitHandle.Register(_viewManagerEventDispatcher);

            _eventDispatcher = new CompositeEventDispatcher(_oldViewManagerEventDispatcher,
                _viewManagerEventDispatcher);
        }

        public TestContext AddViewManager(IOldViewManager viewManager)
        {
            _oldViewManagerEventDispatcher.Add(viewManager);
            return this;
        }

        public TestContext AddViewManager(IViewManager viewManager)
        {
            _viewManagerEventDispatcher.AddViewManager(viewManager);
            return this;
        }

        public CommandProcessingResultWithEvents ProcessCommand<TAggregateRoot>(Command<TAggregateRoot> command) where TAggregateRoot : AggregateRoot, new()
        {
            using (var unitOfWork = BeginUnitOfWork())
            {
                var aggregateRoot = unitOfWork.Get<TAggregateRoot>(command.AggregateRootId);

                command.Execute(aggregateRoot);

                var eventsToReturn = unitOfWork.EmittedEvents.ToList();

                foreach (var e in eventsToReturn)
                {
                    e.Meta.Merge(command.Meta);
                }

                unitOfWork.Commit();

                return new CommandProcessingResultWithEvents(eventsToReturn);
            }
        }

        public TestUnitOfWork BeginUnitOfWork()
        {
            EnsureInitialized();

            return new TestUnitOfWork(_aggregateRootRepository, _eventStore, _oldViewManagerEventDispatcher);
        }

        public void SetCurrentTime(DateTime fixedCurrentTime)
        {
            _currentTime = fixedCurrentTime;
        }

        public IEnumerable<AggregateRoot> AggregateRootsInHistory
        {
            get
            {
                return _eventStore
                    .Select(e => e.GetAggregateRootId()).Distinct()
                    .Select(aggregateRootId =>
                    {
                        var firstEvent = _eventStore.Load(aggregateRootId, 0, 1).First();
                        var typeName = (firstEvent.Meta[DomainEvent.MetadataKeys.Owner] ?? "").ToString();
                        var type = Type.GetType(typeName);

                        if (type == null) return null;

                        var parameters = new object[] { aggregateRootId, new RealUnitOfWork(), long.MaxValue };

                        try
                        {
                            var info = _aggregateRootRepository
                                .GetType()
                                .GetMethod("Get")
                                .MakeGenericMethod(type)
                                .Invoke(_aggregateRootRepository, parameters);

                            return (AggregateRoot)info.GetType().GetProperty("AggregateRoot").GetValue(info);
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException(string.Format("Could not hydrate aggregate root {0} with ID {1}!", type, aggregateRootId), exception);
                        }
                    })
                    .Where(aggregateRoot => aggregateRoot != null);
            }
        }

        void EnsureInitialized()
        {
            if (!_initialized)
            {
                _oldViewManagerEventDispatcher.Initialize(_eventStore, purgeExistingViews: true);
                _viewManagerEventDispatcher.Initialize(_eventStore, purgeExistingViews: true);
                _initialized = true;
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
        public CommandProcessingResultWithEvents Save<TAggregateRoot>(Guid aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            return Save(aggregateRootId, new[] {domainEvent});
        }

        /// <summary>
        /// Saves the given domain events to the history as if they were emitted by the specified aggregate root, immediately dispatching the events to the event dispatcher
        /// </summary>
        public CommandProcessingResultWithEvents Save<TAggregateRoot>(Guid aggregateRootId, params DomainEvent<TAggregateRoot>[] domainEvents) where TAggregateRoot : AggregateRoot
        {
            EnsureInitialized();

            foreach (var domainEvent in domainEvents)
            {
                SetMetadata(aggregateRootId, domainEvent);
            }

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _eventDispatcher.Dispatch(_eventStore, domainEvents);

            return new CommandProcessingResultWithEvents(domainEvents);
        }

        void SetMetadata<TAggregateRoot>(Guid aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            var now = GetNow();

            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _eventStore.GetNextSeqNo(aggregateRootId);
            domainEvent.Meta[DomainEvent.MetadataKeys.Owner] = AggregateRoot.GetOwnerFromType(typeof(TAggregateRoot));
            domainEvent.Meta[DomainEvent.MetadataKeys.TimeUtc] = now.ToString("u");

            domainEvent.Meta.TakeFromAttributes(domainEvent.GetType());
            domainEvent.Meta.TakeFromAttributes(typeof(TAggregateRoot));

            _domainEventSerializer.EnsureSerializability(domainEvent);
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

        /// <summary>
        /// Gets the entire history of commited events from the event store
        /// </summary>
        public EventCollection History
        {
            get { return new EventCollection(_eventStore.Stream()); }
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
                _viewManagerEventDispatcher.Dispose();
            }

            _disposed = true;
        }

        public void WaitForViewToCatchUp<TViewInstance>() where TViewInstance : IViewInstance
        {
            var allGlobalSequenceNumbers = History.Select(h => h.GetGlobalSequenceNumber()).ToArray();

            if (!allGlobalSequenceNumbers.Any()) return;

            var result = CommandProcessingResult.WithNewPosition(allGlobalSequenceNumbers.Max());

            _waitHandle.WaitFor<TViewInstance>(result, TimeSpan.FromSeconds(10)).Wait();
        }
    }

    public class CommandProcessingResultWithEvents : CommandProcessingResult, IEnumerable<DomainEvent>
    {
        readonly List<DomainEvent> _events;

        public CommandProcessingResultWithEvents(IEnumerable<DomainEvent> events)
            : base(events.Any() ? events.Max(e => e.GetGlobalSequenceNumber()) : default(long?))
        {
            _events = events.ToList();
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _events.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}