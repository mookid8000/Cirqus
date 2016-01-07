using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using d60.Cirqus.Logging.Null;

namespace d60.Cirqus.Logging
{
    /// <summary>
    /// Abstract logger factory that can be used to install a global logger factory (by setting <see cref="Current"/>).
    /// Classes that want to log stuff should subscribe to the <see cref="Changed"/> event and (possibly re-)set their
    /// logger instance from the factory passed to the event handler.
    /// </summary>
    public abstract class CirqusLoggerFactory
    {
        static readonly object ChangedHandlersLock = new object();
        static readonly List<Action<CirqusLoggerFactory>> ChangedHandlers = new List<Action<CirqusLoggerFactory>>();

        static CirqusLoggerFactory _current = new NullLoggerFactory();

        /// <summary>
        /// Event that is raised whenever the global logger factory is changed. Also immediately raises the event
        /// for each new subscriber, so that their logger gets initialized.
        /// </summary>
        public static event Action<CirqusLoggerFactory> Changed
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            add
            {
                lock (ChangedHandlersLock)
                {
                    ChangedHandlers.Add(value);
                    value(_current);
                }
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            remove
            {
                lock (ChangedHandlersLock)
                {
                    ChangedHandlers.Remove(value);
                }
            }
        }

        /// <summary>
        /// Gets/sets the global logger factory
        /// </summary>
        public static CirqusLoggerFactory Current
        {
            get { return _current; }
            set
            {
                _current = value ?? new NullLoggerFactory();

                lock (ChangedHandlersLock)
                {
                    foreach (var handler in ChangedHandlers)
                    {
                        handler(_current);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a logger with the calling class's name (takes a walk down the call stack)
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Logger GetCurrentClassLogger()
        {
            var type = new StackFrame(1).GetMethod().DeclaringType;

            return _current.GetLogger(type?.ReflectedType ?? typeof(Logger));
        }

        /// <summary>
        /// Returns a logger with the given <paramref name="ownerType"/> as its name
        /// </summary>
        public abstract Logger GetLogger(Type ownerType);
    }
}