using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using d60.Cirqus.Logging.Null;

namespace d60.Cirqus.Logging
{
    public abstract class CirqusLoggerFactory
    {
        static readonly object ChangedHandlersLock = new object();
        static readonly List<Action<CirqusLoggerFactory>> ChangedHandlers = new List<Action<CirqusLoggerFactory>>();

        static CirqusLoggerFactory _current = new NullLoggerFactory();

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

        public Logger GetCurrentClassLogger()
        {
            for (var frames = 0;; frames++)
            {
                var type = new StackFrame(frames).GetMethod().DeclaringType;

                if (type == typeof (CirqusLoggerFactory)) continue;

                return _current.GetLogger(type);
            }
        }

        public abstract Logger GetLogger(Type ownerType);
    }
}