using System;

namespace d60.Cirqus.Logging.Console
{
    public class ConsoleLoggerFactory : CirqusLoggerFactory
    {
        readonly Logger.Level _minLevel;

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