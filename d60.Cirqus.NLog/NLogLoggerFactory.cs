using System;
using d60.Cirqus.Logging;
using NLog;
using Logger = d60.Cirqus.Logging.Logger;

namespace d60.Cirqus.NLog
{
    public class NLogLoggerFactory : CirqusLoggerFactory
    {
        public override Logger GetLogger(Type ownerType)
        {
            return new NLogLogger(ownerType);
        }

        class NLogLogger : Logger
        {
            readonly global::NLog.Logger _logger;
            
            public NLogLogger(Type ownerType)
            {
                _logger = LogManager.GetLogger(ownerType.FullName);
            }
            
            public override void Debug(string message, params object[] objs)
            {
                _logger.Debug(message, objs);
            }

            public override void Info(string message, params object[] objs)
            {
                _logger.Info(message, objs);
            }

            public override void Warn(string message, params object[] objs)
            {
                _logger.Warn(message, objs);
            }

            public override void Error(string message, params object[] objs)
            {
                _logger.Error(message, objs);
            }
        }
    }
}
