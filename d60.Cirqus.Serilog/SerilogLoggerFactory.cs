using System;
using d60.Cirqus.Logging;
using Serilog;

namespace d60.Cirqus.Serilog
{
    public class SerilogLoggerFactory : CirqusLoggerFactory
    {
        public override Logger GetLogger(Type ownerType)
        {
            return new SerilogLogger(ownerType);
        }

        class SerilogLogger : Logger
        {
            readonly ILogger _innerLogger;

            public SerilogLogger(Type ownerType)
            {
                _innerLogger = Log.Logger.ForContext(ownerType);
            }

            public override void Debug(string message, params object[] objs)
            {
                _innerLogger.Debug(message, objs);
            }

            public override void Info(string message, params object[] objs)
            {
                _innerLogger.Information(message, objs);
            }

            public override void Warn(string message, params object[] objs)
            {
                _innerLogger.Warning(message, objs);
            }

            public override void Warn(Exception exception, string message, params object[] objs)
            {
                _innerLogger.Warning(exception, message, objs);
            }

            public override void Error(string message, params object[] objs)
            {
                _innerLogger.Error(message, objs);
            }

            public override void Error(Exception exception, string message, params object[] objs)
            {
                _innerLogger.Error(exception, message, objs);
            }
        }
    }
}
