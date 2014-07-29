using System;
using System.Collections.Generic;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;

namespace d60.EventSorcerer.MsSql
{
    public class MsSqlEventStore : IEventStore, ISequenceNumberGenerator
    {
        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, int firstSeq = 0, int limit = Int32.MaxValue)
        {
            throw new NotImplementedException();
        }

        public int Next(Guid aggregateRootId)
        {
            throw new NotImplementedException();
        }
    }
}
