using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Event dispatcher wrapper that makes dispatch into an asynchronous operation
    /// </summary>
    public class AsyncEventDispatcher : IEventDispatcher
    {
        readonly IEventDispatcher _innerEventDispatcher;

        public AsyncEventDispatcher(IEventDispatcher innerEventDispatcher)
        {
            _innerEventDispatcher = innerEventDispatcher;
        }

        /// <summary>
        /// Will initialize the wrapped dispatcher synchronously as always
        /// </summary>
        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _innerEventDispatcher.Initialize(eventStore, purgeExistingViews);
        }

        /// <summary>
        /// Delegates the actual dispatch to the thread pool
        /// </summary>
        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            ThreadPool.QueueUserWorkItem(_ => _innerEventDispatcher.Dispatch(eventStore, events));
        }
    }

    public static class AsyncEventDispatcherExtenstions
    {
        /// <summary>
        /// Wraps the given <see cref="IEventDispatcher"/> in an <see cref="AsyncEventDispatcher"/> which
        /// makes the event dispatch into an asynchronous operation
        /// </summary>
        public static IEventDispatcher Asynchronous(this IEventDispatcher eventDispatcher)
        {
            return new AsyncEventDispatcher(eventDispatcher);
        }
    }
}