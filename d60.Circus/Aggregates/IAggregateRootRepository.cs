using System;
using d60.Circus.Events;

namespace d60.Circus.Aggregates
{
    /// <summary>
    /// Repository of aggregate roots.
    /// </summary>
    public interface IAggregateRootRepository
    {
        /// <summary>
        /// Returns a fully hydrated and ready to use aggregate root instance of the specified type. Optionally, if <seealso cref="maxGlobalSequenceNumber"/> is set,
        /// only events up until (and including) the specified sequence number are applied.
        /// </summary>
        AggregateRootInfo<TAggregate> Get<TAggregate>(Guid aggregateRootId, IUnitOfWork unitOfWork, long maxGlobalSequenceNumber = long.MaxValue) where TAggregate : AggregateRoot, new();

        /// <summary>
        /// Checks whether an aggregate root of the specified type with the specified ID exists. Optionally, if <seealso cref="maxGlobalSequenceNumber"/> is set,
        /// it is checked whether the root exists at the given point in time (including the specified sequence number)
        /// </summary>
        bool Exists<TAggregate>(Guid aggregateRootId, long maxGlobalSequenceNumber = long.MaxValue, IUnitOfWork unitOfWork = null) where TAggregate : AggregateRoot;
    }

    public class AggregateRootInfo<TAggregate> where TAggregate : AggregateRoot
    {
        public static AggregateRootInfo<TAggregate> New(TAggregate aggregateRoot)
        {
            return new AggregateRootInfo<TAggregate>(aggregateRoot, -1, -1);
        }

        public static AggregateRootInfo<TAggregate> Old(TAggregate aggregateRoot, long lastSeqNo, long lastGlobalSeqNo)
        {
            return new AggregateRootInfo<TAggregate>(aggregateRoot, lastSeqNo, lastGlobalSeqNo);
        }

        AggregateRootInfo(TAggregate aggregateRoot, long lastSeqNo, long lastGlobalSeqNo)
        {
            AggregateRoot = aggregateRoot;
            LastSeqNo = lastSeqNo;
            LastGlobalSeqNo = lastGlobalSeqNo;
        }

        public TAggregate AggregateRoot { get; private set; }

        public long LastSeqNo { get; private set; }

        public long LastGlobalSeqNo { get; private set; }

        public bool IsNew
        {
            get { return LastSeqNo == -1; }
        }
    }
}