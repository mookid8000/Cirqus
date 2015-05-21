using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Events
{
    [Serializable]
    public abstract class DomainEvent
    {
        /// <summary>
        /// Provides the keys of various predefined metadata elements with special meaning
        /// </summary>
        public static class MetadataKeys
        {
            /// <summary>
            /// Gobal sequence of number that must be globally unique and monotonically increasing with each event in the event store
            /// </summary>
            public const string GlobalSequenceNumber = "gl_seq";

            /// <summary>
            /// Sequence number local to an aggregate root instance
            /// </summary>
            public const string SequenceNumber = "seq";

            /// <summary>
            /// The ID of the aggregate root instance that emitted the event
            /// </summary>
            public const string AggregateRootId = "root_id";

            /// <summary>
            /// ID of the event batch within which this event is contained. Might/might not be logically enforced by the database.
            /// </summary>
            public const string BatchId = "batch_id";

            /// <summary>
            /// UTC time of when the event was emitted
            /// </summary>
            public const string TimeUtc = "time_utc";

            /// <summary>
            /// "Owner" of the event - must be set to the type name of the aggregate root that emitted this event
            /// </summary>
            public const string Owner = "owner";


            /// <summary>
            /// "Emmiter" of the event - must be set to the type name of the aggregate root that emitted this event
            /// </summary>
            public const string Emitter = "emitter";

            /// <summary>
            /// Type of the event - must be set to the name of the event type
            /// </summary>
            public const string Type = "type";

            /// <summary>
            /// Indicates the version number of the root that emitted this event. Can be used in migration scenarios where events emitted by and
            /// aggregate root in a specific version might be treated slightly differently. 
            /// Will be automatically applied to events when the aggregate root class is decorated with the <see cref="MetaAttribute"/> with <see cref="RootVersion"/> as key.
            /// </summary>
            public const string RootVersion = "root_ver";

            /// <summary>
            /// Indicates the version number of the event when it was emitted. Can be used in migration scenarios where events emitted by and
            /// aggregate root in a specific version might be treated slightly differently
            /// Will be automatically applied to events when the event class is decorated with the <see cref="MetaAttribute"/> with <see cref="EventVersion"/> as key.
            /// </summary>
            public const string EventVersion = "evt_ver";

            /// <summary>
            /// If enabled: The name of the type of command that caused this event
            /// </summary>
            public const string CommandTypeName = "command_type";
        }

        protected DomainEvent()
        {
            Meta = new Metadata();
        }

        public Metadata Meta { get; internal set; }

        public override string ToString()
        {
            return string.Format("{0} ({1}/{2}/{3})", 
                GetType().Name, 
                Meta.ContainsKey(MetadataKeys.AggregateRootId) ? Meta[MetadataKeys.AggregateRootId] : "?",
                Meta.ContainsKey(MetadataKeys.SequenceNumber) ? Meta[MetadataKeys.SequenceNumber] : "?",
                Meta.ContainsKey(MetadataKeys.GlobalSequenceNumber) ? Meta[MetadataKeys.GlobalSequenceNumber] : "?");
        }
    }

    [Serializable]
    public abstract class DomainEvent<TOwner> : DomainEvent where TOwner : AggregateRoot
    {
    }
}
