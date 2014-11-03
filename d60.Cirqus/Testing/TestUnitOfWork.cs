using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Views;

namespace d60.Cirqus.Testing
{
    public class TestUnitOfWork : IDisposable
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IEventDispatcher _eventDispatcher;
        readonly JsonDomainEventSerializer _domainEventSerializer;
        readonly RealUnitOfWork _realUnitOfWork;

        bool _wasCommitted;

        internal TestUnitOfWork(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IEventDispatcher eventDispatcher, JsonDomainEventSerializer domainEventSerializer)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _eventDispatcher = eventDispatcher;
            _domainEventSerializer = domainEventSerializer;
            _realUnitOfWork = new RealUnitOfWork(aggregateRootRepository);
        }

        internal RealUnitOfWork RealUnitOfWork
        {
            get { return _realUnitOfWork; }
        }

        internal event Action Committed = delegate { };

        public TAggregateRoot Get<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            return Get<TAggregateRoot>(aggregateRootId.ToString());
        }

        public TAggregateRoot Get<TAggregateRoot>(string aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootInfo = _realUnitOfWork.Get<TAggregateRoot>(aggregateRootId, long.MaxValue, createIfNotExists: true);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            aggregateRoot.UnitOfWork = _realUnitOfWork;

            if (aggregateRootInfo.IsNew)
            {
                aggregateRoot.InvokeCreated();
            }

            return aggregateRoot;
        }

        /// <summary>
        /// Gets the events collected in the current unit of work
        /// </summary>
        public EventCollection EmittedEvents
        {
            get { return new EventCollection(_realUnitOfWork.EmittedEvents); }
        }

        /// <summary>
        /// Commits the current unit of work (i.e. emitted events from <see cref="EmittedEvents"/> are saved
        /// to the history and will be used to hydrate aggregate roots from now on.
        /// </summary>
        public void Commit()
        {
            if (_wasCommitted)
            {
                throw new InvalidOperationException("Cannot commit the same unit of work twice!");
            }

            var domainEvents = EmittedEvents.ToList();

            if (!domainEvents.Any()) return;

            var eventData = domainEvents.Select(e => _domainEventSerializer.Serialize(e)).ToList();

            _eventStore.Save(Guid.NewGuid(), eventData);

            _wasCommitted = true;

            var domainEventsToDispatch = eventData.Select(e => e.DomainEvent);

            _eventDispatcher.Dispatch(_eventStore, domainEventsToDispatch);

            Committed();
        }

        public void Dispose()
        {
            if (!_wasCommitted)
            {
                Console.WriteLine("Unit of work was disposed with {0} events without being committed", EmittedEvents.Count());
            }
        }
    }
}