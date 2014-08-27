using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.TestHelpers
{
    /// <summary>
    /// Use this bad boy to test your CQRS+ES things
    /// </summary>
    public class TestContext
    {
        readonly Serializer _serializer = new Serializer("<events>");
        readonly InMemoryEventStore _eventStore = new InMemoryEventStore();
        readonly DefaultAggregateRootRepository _aggregateRootRepository;
        readonly ViewManagerEventDispatcher _eventDispatcher;

        DateTime _currentTime = DateTime.MinValue;
        bool _initialized;

        public TestContext()
        {
            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);
            _eventDispatcher = new ViewManagerEventDispatcher(_aggregateRootRepository);
        }

        public TestContext AddViewManager(IViewManager viewManager)
        {
            _eventDispatcher.Add(viewManager);
            return this;
        }

        public void ProcessCommand<TAggregateRoot>(Command<TAggregateRoot> command) where TAggregateRoot : AggregateRoot, new()
        {
            var unitOfWork = BeginUnitOfWork();
            var aggregateRoot = unitOfWork.Get<TAggregateRoot>(command.AggregateRootId);

            command.Execute(aggregateRoot);

            unitOfWork.Commit();
        }

        public TestUnitOfWork BeginUnitOfWork()
        {
            EnsureInitialized();

            return new TestUnitOfWork(_aggregateRootRepository, _eventStore, _eventDispatcher);
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

                        var parameters = new object[] {aggregateRootId, new RealUnitOfWork(), long.MaxValue};

                        try
                        {
                            var info = _aggregateRootRepository
                                .GetType()
                                .GetMethod("Get")
                                .MakeGenericMethod(type)
                                .Invoke(_aggregateRootRepository, parameters);

                            return (AggregateRoot) info.GetType().GetProperty("AggregateRoot").GetValue(info);
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
                _eventDispatcher.Initialize(_eventStore, purgeExistingViews: true);
                _initialized = true;
            }
        }

        /// <summary>
        /// Saves the given domain event to the history - requires that the aggregate root ID has been added in the event's metadata under the <see cref="DomainEvent.MetadataKeys.AggregateRootId"/> key
        /// </summary>
        public void Save<TAggregateRoot>(DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            if (!domainEvent.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Canno save domain event {0} because it does not have an aggregate root ID! Use the Save(id, event) overload or make sure that the '{1}' metadata key has been set",
                        domainEvent, DomainEvent.MetadataKeys.AggregateRootId));
            }

            Save(domainEvent.GetAggregateRootId(), domainEvent);
        }

        /// <summary>
        /// Saves the given domain event to the history as if it was emitted by the specified aggregate root, immediately dispatching the event to the event dispatcher
        /// </summary>
        public void Save<TAggregateRoot>(Guid aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            EnsureInitialized();

            SetMetadata(aggregateRootId, domainEvent);

            var domainEvents = new[] { domainEvent };

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _eventDispatcher.Dispatch(_eventStore, domainEvents);
        }

        void SetMetadata<TAggregateRoot>(Guid aggregateRootId, DomainEvent<TAggregateRoot> domainEvent) where TAggregateRoot : AggregateRoot
        {
            var now = GetNow();

            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _eventStore.GetNextSeqNo(aggregateRootId);
            domainEvent.Meta[DomainEvent.MetadataKeys.Owner] = AggregateRoot.GetOwnerFromType(typeof(TAggregateRoot));
            domainEvent.Meta[DomainEvent.MetadataKeys.TimeUtc] = now;

            domainEvent.Meta.TakeFromAttributes(domainEvent.GetType());
            domainEvent.Meta.TakeFromAttributes(typeof(TAggregateRoot));

            _serializer.EnsureSerializability(domainEvent);
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
    }
}