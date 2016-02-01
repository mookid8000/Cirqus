using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Snapshotting.New
{
    public class Snapshot
    {
        public Snapshot(long validFromGlobalSequenceNumber, string aggregateRootId, AggregateRoot instance)
        {
            ValidFromGlobalSequenceNumber = validFromGlobalSequenceNumber;
            AggregateRootId = aggregateRootId;
            Instance = instance;
        }

        public long ValidFromGlobalSequenceNumber { get; private set; }
        public string AggregateRootId { get; private set; }
        public AggregateRoot Instance { get; private set; }
    }
}