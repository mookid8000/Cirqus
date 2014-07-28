namespace d60.EventSorcerer.Config
{
    public class EventSorcererOptions
    {
        public EventSorcererOptions()
        {

        }

        ///// <summary>
        ///// Whether to use a lock when processing commands for an aggregate, which can be enabled in order to minimize the risk of having to perform retries due to
        ///// optimistic concurrency exceptions. Note that optimistic concurrency will still be at play if multiple processes are executing commands - this
        ///// locking is purely an in-process optimization.
        ///// </summary>
        //public bool UseInProcessLockingOfAggregates { get; set; } 
    }
}