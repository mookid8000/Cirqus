using System;
using d60.Cirqus.Logging;
using Serilog;

namespace d60.Cirqus.Serilog
{
    public class SerilogLoggerFactory : CirqusLoggerFactory
    {
        readonly Func<Type, ILogger> _loggerFactory; 

        public SerilogLoggerFactory()
        {
            _loggerFactory = Log.ForContext;
        }

        public SerilogLoggerFactory(LoggerConfiguration configuration)
        {
            _loggerFactory = type => configuration.CreateLogger().ForContext(type);
        }

        public SerilogLoggerFactory(ILogger baseLogger)
        {
            _loggerFactory = baseLogger.ForContext;
        }

        public override Logger GetLogger(Type ownerType)
        {
            return new SerilogLogger(_loggerFactory(ownerType));
        }

        class SerilogLogger : Logger
        {
            readonly ILogger _innerLogger;

            public SerilogLogger(ILogger innerLogger)
            {
                _innerLogger = innerLogger;
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
