using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class InMemoryEventStore : IEventStore, IEnumerable<DomainEvent>, ISequenceNumberGenerator
    {
        public readonly List<EventBatch> SavedEventBatches = new List<EventBatch>();

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            EventValidation.ValidateBatchIntegrity(batchId, eventList);

            var tuplesInBatch = eventList
                .Select(e => new
                {
                    AggregateRootId = e.GetAggregateRootId(),
                    Seq = e.GetSeq()
                })
                .Distinct();

            var tuplesAlreadyInStore = SavedEventBatches
                .SelectMany(e => e.Events)
                .Select(e => new
                {
                    AggregateRootId = e.GetAggregateRootId(),
                    Seq = e.GetSeq()
                })
                .Distinct();

            var overlaps = tuplesInBatch.Intersect(tuplesAlreadyInStore);

            if (overlaps.Any())
            {
                throw new ConcurrencyException(batchId, eventList, null);
            }

            SavedEventBatches.Add(new EventBatch(batchId, eventList));
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, int firstSeq = 0, int limit = int.MaxValue/2)
        {
            long maxSequenceNumber = firstSeq + limit;

            return this
                .Select(e => new
                {
                    Event = e,
                    AggregateRootId = e.GetAggregateRootId(),
                    SequenceNumber = e.GetSeq()
                })
                .Where(e => e.AggregateRootId == aggregateRootId)
                .Where(e => e.SequenceNumber >= firstSeq && e.SequenceNumber < maxSequenceNumber)
                .OrderBy(e => e.SequenceNumber)
                .Select(e => e.Event);
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return SavedEventBatches.SelectMany(b => b.Events).GetEnumerator();
        }

        public int Next(Guid aggregateRootId)
        {
            var domainEvents = SavedEventBatches
                .SelectMany(b => b.Events)
                .Where(e => e.GetAggregateRootId() == aggregateRootId)
                .ToList();

            return domainEvents.Any()
                ? domainEvents.Max(e => e.GetSeq()) + 1
                : 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class EventBatch
    {
        public EventBatch(Guid batchId, IEnumerable<DomainEvent> events)
        {
            BatchId = batchId;
            Events = events.ToList();
        }

        public readonly Guid BatchId;
        public List<DomainEvent> Events;
    }
}