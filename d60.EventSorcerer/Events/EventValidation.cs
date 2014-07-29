using System;
using System.Collections.Generic;
using System.Linq;

namespace d60.EventSorcerer.Events
{
    public class EventValidation
    {
        public static void ValidateBatchIntegrity(Guid batchId, List<DomainEvent> events)
        {
            EnsureAllEventsHaveSequenceNumbers(events);

            EnsureAllEventsHaveAggregateRootId(events);

            EnsureSeq(batchId, events);
        }

        static void EnsureAllEventsHaveAggregateRootId(List<DomainEvent> events)
        {
            if (events.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.AggregateRootId)))
            {
                throw new InvalidOperationException("Can't save batch with event without an aggregate root id");
            }
        }

        static void EnsureAllEventsHaveSequenceNumbers(List<DomainEvent> events)
        {
            if (events.Any(e => !e.Meta.ContainsKey(DomainEvent.MetadataKeys.SequenceNumber)))
            {
                throw new InvalidOperationException("Can't save batch with event without a sequence number");
            }
        }

        static void EnsureSeq(Guid batchId, List<DomainEvent> events)
        {
            var seqs = events
                .GroupBy(e => GetAggregateRootId(batchId, e))
                .ToDictionary(g => g.Key, g => g.Min(e => GetSeq(batchId, e)));

            foreach (var e in events)
            {
                var sequenceNumberOfThisEvent = GetSeq(batchId, e);
                var aggregateRootId = GetAggregateRootId(batchId, e);
                var expectedSequenceNumber = seqs[aggregateRootId];

                if (sequenceNumberOfThisEvent != expectedSequenceNumber)
                {
                    throw new InvalidOperationException(
                        string.Format(@"Attempted to save batch {0} which contained events with non-sequential sequence numbers!

{1}", batchId,
                            string.Join(Environment.NewLine,
                                events.Select(
                                    ev => string.Format("    {0} / {1}", GetAggregateRootId(batchId, ev), GetSeq(batchId, ev))))));
                }

                seqs[aggregateRootId]++;
            }
        }

        static Guid GetAggregateRootId(Guid batchId, DomainEvent e)
        {
            object id;

            if (!e.Meta.TryGetValue(DomainEvent.MetadataKeys.AggregateRootId, out id))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to save event batch {0} but one or more events was not equipped with an aggregate root ID!",
                        batchId));
            }

            try
            {
                return new Guid(Convert.ToString(id));
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not cast aggregate root id '{0}' to a Guid", id), exception);
            }
        }

        static int GetSeq(Guid batchId, DomainEvent e)
        {
            object seq;

            if (!e.Meta.TryGetValue(DomainEvent.MetadataKeys.SequenceNumber, out seq))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Attempted to save event batch {0} but one or more events was not equipped with a sequence number!",
                        batchId));
            }

            try
            {
                return (int)seq;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not cast sequence number '{0}' to an int", seq), exception);
            }
        }
    }
}