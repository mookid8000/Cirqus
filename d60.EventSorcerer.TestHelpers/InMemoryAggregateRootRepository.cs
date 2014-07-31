using System;
using System.Collections;
using System.Collections.Generic;
using d60.EventSorcerer.Aggregates;

namespace d60.EventSorcerer.TestHelpers
{
    public class InMemoryAggregateRootRepository : IAggregateRootRepository, IEnumerable<AggregateRoot>
    {
        readonly Dictionary<Guid, AggregateRoot> _aggregateRoots = new Dictionary<Guid, AggregateRoot>();

        public TAggregate Get<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot, new()
        {
            AggregateRoot toReturn;

            if (!_aggregateRoots.TryGetValue(aggregateRootId, out toReturn))
            {
                toReturn = new TAggregate();
                toReturn.Initialize(aggregateRootId, this);
                _aggregateRoots[aggregateRootId] = toReturn;
                return (TAggregate)toReturn;
            }

            try
            {
                return (TAggregate) toReturn;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    string.Format("Attempted to return aggregate root {0} as if it was a {1}, but it's a {2}",
                        aggregateRootId, typeof (TAggregate), toReturn.GetType()), exception);
            }
        }

        public bool Exists<TAggregate>(Guid aggregateRootId) where TAggregate : AggregateRoot
        {
            return _aggregateRoots.ContainsKey(aggregateRootId)
                   && _aggregateRoots[aggregateRootId] is TAggregate;
        }

        public IEnumerator<AggregateRoot> GetEnumerator()
        {
            return _aggregateRoots.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}