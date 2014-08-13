using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Tests
{
    static class TestExtensions
    {
        public static IEnumerable<long> GetSeq(this IEnumerable<DomainEvent> events)
        {
            return events.Select(e => e.GetSequenceNumber());
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