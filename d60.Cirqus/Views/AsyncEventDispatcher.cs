using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Views
{
    /// <summary>
    /// Event dispatcher wrapper that makes dispatch into an asynchronous operation
    /// </summary>
    public class AsyncEventDispatcher : IEventDispatcher
    {
        static Logger _logger;

        static AsyncEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly BlockingCollection<Action> _work = new BlockingCollection<Action>();
        readonly IEventDispatcher _innerEventDispatcher;
        readonly Thread _dispatcherThread;
        bool _keepWorking = true;

        public AsyncEventDispatcher(IEventDispatcher innerEventDispatcher)
        {
            _innerEventDispatcher = innerEventDispatcher;

            _dispatcherThread = new Thread(Run) { IsBackground = true };
        }

        /// <summary>
        /// Will initialize the wrapped dispatcher asynchronously, delegating the initialization to the worker thread
        /// </summary>
        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _work.Add(() => _innerEventDispatcher.Initialize(eventStore, purgeExistingViews));

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
            while (_keepWorking)
            {
                var action = _work.Take();

                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    _logger.Warn("An error occurred while attempting to do work {0}: {1} - the worker thread will shut down now", action, exception);

                    _keepWorking = false;
                }
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