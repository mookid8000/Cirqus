using System;
using System.Collections.Generic;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Tests.Stubs
{
    public class ListLoggerFactory : CirqusLoggerFactory
    {
        readonly List<LogLine>_loggedLines = new List<LogLine>();

        public class LogLine
        {
            public LogLine(Logger.Level level, string text, Type ownerType)
            {
                Level = level;
                Text = text;
                OwnerType = ownerType;
                Time = DateTime.Now;
            }

            public DateTime Time { get; private set; }
            public Logger.Level Level { get; private set; }
            public string Text { get; private set; }
            public Type OwnerType { get; private set; }

            public override string ToString()
            {
                return string.Format("{0:O}|{1}|{2}|{3}", Time, Level, OwnerType.FullName, Text);
            }
        }

        public IEnumerable<LogLine> LoggedLines
        {
            get { return _loggedLines; }
        }

        public override Logger GetLogger(Type ownerType)
        {
            var logger = new LineEmittingLogger(ownerType);
            logger.Logged += _loggedLines.Add;
            return logger;
        }

        class LineEmittingLogger : Logger
        {
            readonly Type _ownerType;

            public LineEmittingLogger(Type ownerType)
            {
                _ownerType = ownerType;
            }

            public event Action<LogLine> Logged;

            public override void Debug(string message, params object[] objs)
            {
                Emit(Level.Debug, string.Format(message, objs));
            }

            public override void Info(string message, params object[] objs)
            {
                Emit(Level.Info, string.Format(message, objs));
            }

            public override void Warn(string message, params object[] objs)
            {
                Emit(Level.Warn, string.Format(message, objs));
            }

            public override void Error(string message, params object[] objs)
            {
                Emit(Level.Error, string.Format(message, objs));
            }

            void Emit(Level level, string text)
            {
                if (Logged == null) return;

                Logged(new LogLine(level, text, _ownerType));
            }
        }
    }
}