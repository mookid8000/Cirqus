using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Config;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.TestHelpers
{
    /// <summary>
    /// Use this bad boy to test your CQRS+ES things
    /// </summary>
    public class TestContext
    {
        readonly InMemoryEventCollector _eventCollector = new InMemoryEventCollector();
        readonly InMemoryEventStore _eventStore = new InMemoryEventStore();
        readonly BasicAggregateRootRepository _aggregateRootRepository;

        public TestContext()
        {
            _aggregateRootRepository = new BasicAggregateRootRepository(_eventStore);
        }

        public TAggregateRoot Create<TAggregateRoot>() where TAggregateRoot : AggregateRoot, new()
        {
            var aggregateRoot = _aggregateRootRepository.Get<TAggregateRoot>(Guid.NewGuid());
            
            aggregateRoot.EventCollector = _eventCollector;
            aggregateRoot.SequenceNumberGenerator = new CachingSequenceNumberGenerator(_eventStore);

            return aggregateRoot;
        }

        /// <summary>
        /// Commits the current unit of work (i.e. emitted events from <see cref="UnitOfWork"/> are saved
        /// to the history and will be used to hydrate aggregate roots from now on.
        /// </summary>
        public void Commit()
        {
            var domainEvents = UnitOfWork.ToList();

            _eventStore.Save(Guid.NewGuid(), domainEvents);

            _eventCollector.Clear();
        }

        /// <summary>
        /// Gets the events collected in the current unit of work
        /// </summary>
        public IEnumerable<DomainEvent> UnitOfWork
        {
            get { return _eventCollector.ToList(); }
        }

        /// <summary>
        /// Gets the entire history of commited events from the event store
        /// </summary>
        public IEnumerable<DomainEvent> History
        {
            get { return _eventStore.Stream().ToList(); }
        }

    }
}