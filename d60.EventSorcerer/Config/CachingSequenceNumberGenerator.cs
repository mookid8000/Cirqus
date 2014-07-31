using System;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Config
{
    class CachingSequenceNumberGenerator : ISequenceNumberGenerator
    {
        readonly IEventStore _eventStore;
        readonly Dictionary<Guid, long> _next = new Dictionary<Guid, long>();

        public CachingSequenceNumberGenerator(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public long Next(Guid aggregateRootId)
        {
            if (_next.ContainsKey(aggregateRootId))
                return _next[aggregateRootId]++;

            _next[aggregateRootId] = _eventStore.GetNextSeqNo(aggregateRootId);

            return _next[aggregateRootId]++;
        }
    }
}