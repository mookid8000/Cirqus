using System;

namespace d60.Cirqus.Logging.Null
{
    class NullLoggerFactory : CirqusLoggerFactory
    {
        static readonly NullLogger Instance = new NullLogger();

        public override Logger GetLogger(Type ownerType)
        {
            return Instance;
        }

        class NullLogger : Logger
        {
            public override void Debug(string message, params object[] objs)
            {
            }

            public override void Info(string message, params object[] objs)
            {
            }

            public override void Warn(string message, params object[] objs)
            {
            }

            public override void Warn(Exception exception, string message, params object[] objs)
            {
            }

            public override void Error(string message, params object[] objs)
            {
            }

            public override void Error(Exception exception, string message, params object[] objs)
            {
            }
        }
    }
}