using d60.Cirqus.Aggregates;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    public abstract class DomainEvent
    {
        public static class MetadataKeys
        {
            public const string GlobalSequenceNumber = "gl_seq";
            public const string SequenceNumber = "seq";
            public const string AggregateRootId = "root_id";
            public const string TimeLocal = "time_local";
            public const string TimeUtc = "time_utc";
            public const string Owner = "owner";
            public const string RootVersion = "root_ver";
            public const string EventVersion = "evt_ver";
        }

        public readonly Metadata Meta = new Metadata();

        internal void AssignSequenceNumber(int seq)
        {
            Meta[MetadataKeys.SequenceNumber] = seq;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1}/{2}/{3})", 
                GetType().Name, 
                Meta.ContainsKey(MetadataKeys.AggregateRootId) ? Meta[MetadataKeys.AggregateRootId] : "?",
                Meta.ContainsKey(MetadataKeys.SequenceNumber) ? Meta[MetadataKeys.SequenceNumber] : "?",
                Meta.ContainsKey(MetadataKeys.GlobalSequenceNumber) ? Meta[MetadataKeys.GlobalSequenceNumber] : "?");
        }
    }

    public abstract class DomainEvent<TOwner> : DomainEvent where TOwner : AggregateRoot
    {
    }
}
