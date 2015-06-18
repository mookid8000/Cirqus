using System;

namespace d60.Cirqus.Logging.Console
{
    /// <summary>
    /// <see cref="CirqusLoggerFactory"/> that logs to standard output
    /// </summary>
    public class ConsoleLoggerFactory : CirqusLoggerFactory
    {
        readonly Logger.Level _minLevel;

        /// <summary>
        /// Constructs the factory
        /// </summary>
        public ConsoleLoggerFactory(Logger.Level minLevel = Logger.Level.Info)
        {
            _minLevel = minLevel;
        }

        public override Logger GetLogger(Type ownerType)
        {
            return new ConsoleLogger(ownerType, _minLevel);
        }
    }
}