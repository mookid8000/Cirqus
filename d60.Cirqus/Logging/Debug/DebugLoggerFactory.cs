using System;

namespace d60.Cirqus.Logging.Debug
{
    /// <summary>
    /// An implementation of <see cref="CirqusLoggerFactory"/> that logs to Debug. Useful in web projects.
    /// </summary>
    public class DebugLoggerFactory: CirqusLoggerFactory
    {
        readonly Logger.Level _minLevel;

        /// <summary>
        /// Creates the logger factory
        /// </summary>
        public DebugLoggerFactory(Logger.Level minLevel = Logger.Level.Debug)
        {
            _minLevel = minLevel;
        }

        public override Logger GetLogger(Type ownerType)
        {
            return new DebugLogger(ownerType, _minLevel);
        }
    }
}