using System;
using System.Collections.Generic;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.Config
{
    class CachingSequenceNumberGenerator : ISequenceNumberGenerator
    {
        readonly Dictionary<Guid, int> _next = new Dictionary<Guid, int>();
        readonly ISequenceNumberGenerator _sequenceNumberGenerator;

        public CachingSequenceNumberGenerator(ISequenceNumberGenerator sequenceNumberGenerator)
        {
            _sequenceNumberGenerator = sequenceNumberGenerator;
        }

        public int Next(Guid aggregateRootId)
        {
            if (_next.ContainsKey(aggregateRootId))
                return _next[aggregateRootId]++;

            _next[aggregateRootId] = _sequenceNumberGenerator.Next(aggregateRootId);

            return _next[aggregateRootId]++;
        }
    }
}