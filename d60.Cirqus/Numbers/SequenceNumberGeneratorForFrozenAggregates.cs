using System;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Numbers
{
    class SequenceNumberGeneratorForFrozenAggregates<TAggregateRoot> : ISequenceNumberGenerator where TAggregateRoot : AggregateRoot
    {
        readonly AggregateRootInfo<TAggregateRoot> _aggregateRootInfo;

        public SequenceNumberGeneratorForFrozenAggregates(AggregateRootInfo<TAggregateRoot> aggregateRootInfo)
        {
            _aggregateRootInfo = aggregateRootInfo;
        }


        public long Next()
        {
            var message = string.Format("Frozen aggregate root of type {0} with ID {1} (seq no: {2}, global seq no: {3}) attempted" +
                                        " to get a sequence number for an event which is an implication that an operation was performed," +
                                        " which is NOT allowed on frozen aggregates",
                typeof (TAggregateRoot), _aggregateRootInfo.AggregateRoot.Id, _aggregateRootInfo.LastSeqNo, _aggregateRootInfo.LastGlobalSeqNo);

            throw new InvalidOperationException(message);
        }
    }
}