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
            if (batch.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.SequenceNumber)))
            {
                throw new InvalidOperationException("Can't save batch with event without a sequence number");
            }

            if (batch.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId)))
            {
                throw new InvalidOperationException("Can't save batch with event without an aggregate root id");
            }

            ValidateSequenceNumbers(batchId, batch.ToList());

            var tuplesInBatch = batch
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
                throw new ConcurrencyException(batchId, new int[0], null);
            }

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

        void ValidateSequenceNumbers(Guid batchId, List<DomainEvent> events)
        {
            var seqs = events
                .GroupBy(e => e.GetAggregateRootId())
                .ToDictionary(g => g.Key, g => g.Min(e => e.GetSeq()));

            foreach (var e in events)
            {
                var sequenceNumberOfThisEvent = e.GetSeq();
                var aggregateRootId = e.GetAggregateRootId();
                var expectedSequenceNumber = seqs[aggregateRootId];

                if (sequenceNumberOfThisEvent != expectedSequenceNumber)
                {
                    throw new InvalidOperationException(string.Format(@"Attempted to save batch {0} which contained events with non-sequential sequence numbers!

{1}", batchId, string.Join(Environment.NewLine, events.Select(ev => string.Format("    {0} / {1}", ev.GetAggregateRootId(), ev.GetSeq())))));
                }

                seqs[aggregateRootId]++;
            }
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