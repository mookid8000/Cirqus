using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.TestHelpers.Internals
{
    class InMemoryEventStore : IEventStore, IEnumerable<DomainEvent>
    {
        readonly Dictionary<string, object> _idAndSeqNoTuples = new Dictionary<string, object>();
        readonly Serializer _serializer = new Serializer("<events>");
        readonly List<EventBatch> _savedEventBatches = new List<EventBatch>();

        long _globalSequenceNumber;

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            eventList.ForEach(e => _serializer.EnsureSerializability(e));

            var tuplesInBatch = eventList
                .Select(e => string.Format("{0}:{1}", e.GetAggregateRootId(), e.GetSequenceNumber()))
                .ToList();

            try
            {
                foreach (var tuple in tuplesInBatch)
                {
                    if (_idAndSeqNoTuples.ContainsKey(tuple))
                    {
                        throw new InvalidOperationException(string.Format("Found duplicate: {0}", tuple));
                    }
                }

                foreach (var tuple in tuplesInBatch)
                {
                    _idAndSeqNoTuples.Add(tuple, null);
                }
            }
            catch (Exception exception)
            {
                throw new ConcurrencyException(batchId, eventList, exception);
            }

            var sequenceNumbersToAllocate = eventList.Count;

            var result = Interlocked.Add(ref _globalSequenceNumber, sequenceNumbersToAllocate);

            result -= sequenceNumbersToAllocate;

            foreach (var e in eventList)
            {
                e.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = result++;
            }

            EventValidation.ValidateBatchIntegrity(batchId, eventList);

            _savedEventBatches.Add(new EventBatch(batchId, eventList.Select(CloneEvent)));
        }

        DomainEvent CloneEvent(DomainEvent ev)
        {
            return _serializer.Deserialize(_serializer.Serialize(ev));
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0, long limit = int.MaxValue/2)
        {
            long maxSequenceNumber = firstSeq + limit;

            return this
                .Select(e => new
                {
                    Event = e,
                    AggregateRootId = e.GetAggregateRootId(),
                    SequenceNumber = e.GetSequenceNumber()
                })
                .Where(e => e.AggregateRootId == aggregateRootId)
                .Where(e => e.SequenceNumber >= firstSeq && e.SequenceNumber < maxSequenceNumber)
                .OrderBy(e => e.SequenceNumber)
                .Select(e => CloneEvent(e.Event));
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            return _savedEventBatches.SelectMany(b => b.Events).GetEnumerator();
        }

        public long GetNextSeqNo(Guid aggregateRootId)
        {
            var domainEvents = _savedEventBatches
                .SelectMany(b => b.Events)
                .Where(e => e.GetAggregateRootId() == aggregateRootId)
                .ToList();

            return domainEvents.Any()
                ? domainEvents.Max(e => e.GetSequenceNumber()) + 1
                : 0;
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            return _savedEventBatches
                .SelectMany(e => e.Events)
                .Select(e => new
                {
                    Event = e,
                    GlobalSequenceNumner = e.GetGlobalSequenceNumber()
                })
                .Where(a => a.GlobalSequenceNumner >= globalSequenceNumber)
                .OrderBy(a => a.GlobalSequenceNumner)
                .Select(a => CloneEvent(a.Event));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class EventBatch
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
}