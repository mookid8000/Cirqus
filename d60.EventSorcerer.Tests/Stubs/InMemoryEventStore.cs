using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Tests.Stubs
{
    public class InMemoryEventStore : IEventStore, IEnumerable<DomainEvent>
    {
        public readonly List<EventBatch> SavedEventBatches = new List<EventBatch>();
        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            SavedEventBatches.Add(new EventBatch(batchId, batch));
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
                .Select(e => e.Event);
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return SavedEventBatches.SelectMany(b => b.Events).GetEnumerator();
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