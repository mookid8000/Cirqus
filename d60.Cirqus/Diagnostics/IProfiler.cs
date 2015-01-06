using System;

namespace d60.Cirqus.Diagnostics
{
    /// <summary>
    /// Core profiler interface. Implementor can be hooked into core operations to allow for recording time spent doing various operations.
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Called after getting each aggregate root instance from the repository, no matter if the instance was fully re-hydrated or served from a unit of work cache, etc.
        /// </summary>
        void RecordAggregateRootGet(TimeSpan elapsed, Type aggregateRootType, string aggregateRootId);

        /// <summary>
        /// Called after checking for the existence of an aggregate root instance
        /// </summary>
        void RecordAggregateRootExists(TimeSpan elapsed, string aggregateRootId);
        
        /// <summary>
        /// Called after saving each event batch to the event store
        /// </summary>
        void RecordEventBatchSave(TimeSpan elapsed, Guid batchId);
        
        /// <summary>
        /// Called after getting the next global sequence number from the event store
        /// </summary>
        void RecordGlobalSequenceNumberGetNext(TimeSpan elapsed);

        /// <summary>
        /// Called after dispatching a successfully saved event batch
        /// </summary>
        void RecordEventDispatch(TimeSpan elapsed);
    }
}