using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Views;

namespace d60.Cirqus.Testing
{
    public class TestUnitOfWork : IDisposable
    {
        readonly IAggregateRootRepository _aggregateRootRepository;
        readonly IEventStore _eventStore;
        readonly IEventDispatcher _eventDispatcher;
        readonly RealUnitOfWork _realUnitOfWork;

        bool _wasCommitted;

        internal TestUnitOfWork(IAggregateRootRepository aggregateRootRepository, IEventStore eventStore, IEventDispatcher eventDispatcher)
        {
            _aggregateRootRepository = aggregateRootRepository;
            _eventStore = eventStore;
            _eventDispatcher = eventDispatcher;
            _realUnitOfWork = new RealUnitOfWork(aggregateRootRepository);
        }

        internal RealUnitOfWork RealUnitOfWork
        {
            get { return _realUnitOfWork; }
        }

        internal event Action Committed = delegate { }; 

        public TAggregateRoot Get<TAggregateRoot>(Guid aggregateRootId) where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRootFromCache = _realUnitOfWork.GetAggregateRootFromCache<TAggregateRoot>(aggregateRootId, long.MaxValue);
            if (aggregateRootFromCache != null)
            {
                return aggregateRootFromCache;
            }

            var aggregateRootInfo = _aggregateRootRepository.Get<TAggregateRoot>(aggregateRootId, _realUnitOfWork);
            var aggregateRoot = aggregateRootInfo.AggregateRoot;

            _realUnitOfWork.AddToCache(aggregateRoot, long.MaxValue);

            aggregateRoot.UnitOfWork = _realUnitOfWork;

            _realUnitOfWork.AddToCache(aggregateRoot, aggregateRootInfo.LastGlobalSeqNo);

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

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _wasCommitted = true;

            _eventDispatcher.Dispatch(_eventStore, domainEvents);

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