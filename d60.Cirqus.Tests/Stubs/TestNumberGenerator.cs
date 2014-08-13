using System;
using System.Collections.Generic;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Tests.Stubs
{
    public class TestNumberGenerator
    {
        readonly Dictionary<Guid, long> _aggregateRootSequenceNumbers = new Dictionary<Guid, long>();
        long _globalSequenceNumber;

        public void AssignNumbers(DomainEvent domainEvent)
        {
            var aggregateRootId = domainEvent.GetAggregateRootId();

            if (!_aggregateRootSequenceNumbers.ContainsKey(aggregateRootId))
            {
                _aggregateRootSequenceNumbers[aggregateRootId] = 0;
            }

            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = _aggregateRootSequenceNumbers[aggregateRootId]++;
            domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = _globalSequenceNumber++;
        }
    }

    public static class TestNumberGeneratorEx
    {
        public static TDomainEvent NumberedWith<TDomainEvent>(this TDomainEvent domainEvent, TestNumberGenerator generator) where TDomainEvent : DomainEvent
        {
            generator.AssignNumbers(domainEvent);

            return domainEvent;
        }
    }
}