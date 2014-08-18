using System;
using System.Collections.Concurrent;
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
        readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();
        readonly IEventDispatcher _innerEventDispatcher;
        readonly Thread _dispatcherThread;

        public AsyncEventDispatcher(IEventDispatcher innerEventDispatcher)
        {
            _innerEventDispatcher = innerEventDispatcher;

            _dispatcherThread = new Thread(Run) { IsBackground = true };
        }

        /// <summary>
        /// Will initialize the wrapped dispatcher synchronously as always
        /// </summary>
        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _innerEventDispatcher.Initialize(eventStore, purgeExistingViews);

            _dispatcherThread.Start();
        }

        /// <summary>
        /// Delegates the actual dispatch to the worker thread
        /// </summary>
        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
            _work.Add(() => _innerEventDispatcher.Dispatch(eventStore, events));
        }

        void Run()
        {
            while (true)
            {
                var action = _work.Take();

                action();
            }
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