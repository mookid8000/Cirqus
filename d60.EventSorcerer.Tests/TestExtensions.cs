using System;
using System.Collections.Generic;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Tests
{
    static class TestExtensions
    {
        public static IEnumerable<int> GetSeq(this IEnumerable<DomainEvent> events)
        {
            return events.Select(e => e.GetSeq());
        }

        public static int GetSeq(this DomainEvent e)
        {
            return Convert.ToInt32(e.Meta[DomainEvent.MetadataKeys.SequenceNumber]);
        }
        public static Guid GetAggregateRootId(this DomainEvent e)
        {
            return new Guid(Convert.ToString(e.Meta[DomainEvent.MetadataKeys.AggregateRootId]));
        }

        public static void Times(this int iterations, Action action)
        {
            for (var counter = 0; counter < iterations; counter++)
            {
                action();
            }
        }
    }
}