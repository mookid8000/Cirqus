using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;

namespace d60.Cirqus.Testing.Internals
{
    class InMemoryEventStore : IEventStore, IEnumerable<DomainEvent>
    {
        readonly Dictionary<string, object> _idAndSeqNoTuples = new Dictionary<string, object>();
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer("<events>");
        readonly List<EventBatch> _savedEventBatches = new List<EventBatch>();
        readonly object _lock = new object();

        long _globalSequenceNumber;

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            var eventList = batch.ToList();

            eventList.ForEach(e => _domainEventSerializer.EnsureSerializability(e));

            var tuplesInBatch = eventList
                .Select(e => string.Format("{0}:{1}", e.GetAggregateRootId(), e.GetSequenceNumber()))
                .ToList();

            lock (_lock)
            {
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
                    e.Meta[DomainEvent.MetadataKeys.BatchId] = batchId;
                }

                EventValidation.ValidateBatchIntegrity(batchId, eventList);

                _savedEventBatches.Add(new EventBatch(batchId, eventList.Select(CloneEvent)));
            }
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _globalSequenceNumber;
        }

        public void Save(Guid batchId, IEnumerable<Event> events)
        {
        }

        public IEnumerable<Event> LoadNew(Guid aggregateRootId, long firstSeq = 0)
        {
            return Enumerable.Empty<Event>();
        }

        public IEnumerable<Event> StreamNew(long globalSequenceNumber = 0)
        {
            return Enumerable.Empty<Event>();
        }

        DomainEvent CloneEvent(DomainEvent ev)
        {
            return _domainEventSerializer.Deserialize(_domainEventSerializer.Serialize(ev));
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            lock (_lock)
            {
                return this
                    .Select(e => new
                    {
                        Event = e,
                        AggregateRootId = e.GetAggregateRootId(),
                        SequenceNumber = e.GetSequenceNumber()
                    })
                    .Where(e => e.AggregateRootId == aggregateRootId)
                    .Where(e => e.SequenceNumber >= firstSeq)
                    .OrderBy(e => e.SequenceNumber)
                    .Select(e => CloneEvent(e.Event))
                    .ToList();
            }
        }

        public IEnumerator<DomainEvent> GetEnumerator()
        {
            lock (_lock)
            {
                var clone = _savedEventBatches.SelectMany(b => b.Events).ToList();

                return clone.GetEnumerator();
            }
        }

        public long GetNextSeqNo(Guid aggregateRootId)
        {
            lock (_lock)
            {
                var domainEvents = _savedEventBatches
                    .SelectMany(b => b.Events)
                    .Where(e => e.GetAggregateRootId() == aggregateRootId)
                    .ToList();

                return domainEvents.Any()
                    ? domainEvents.Max(e => e.GetSequenceNumber()) + 1
                    : 0;
            }
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            lock (_lock)
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
                    .Select(a => CloneEvent(a.Event))
                    .ToList();
            }
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