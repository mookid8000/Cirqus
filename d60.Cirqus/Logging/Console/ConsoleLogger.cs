using System;

namespace d60.Cirqus.Logging.Console
{
    /// <summary>
    /// Logger implementation that logs to standard output
    /// </summary>
    public class ConsoleLogger : Logger
    {
        readonly Type _ownerType;
        readonly Level _minLevel;

        internal ConsoleLogger(Type ownerType, Level minLevel = Level.Info)
        {
            _ownerType = ownerType;
            _minLevel = minLevel;
        }

        public override void Debug(string message, params object[] objs)
        {
            Write(Level.Debug, SafeFormat(message, objs));
        }

        public override void Info(string message, params object[] objs)
        {
            Write(Level.Info, SafeFormat(message, objs));
        }

        public override void Warn(string message, params object[] objs)
        {
            Write(Level.Warn, SafeFormat(message, objs));
        }

        public override void Warn(Exception exception, string message, params object[] objs)
        {
            var text = SafeFormat(message, objs);

            Write(Level.Warn, SafeFormat("{0} - exception: {1}", text, exception));
        }

        public override void Error(string message, params object[] objs)
        {
            Write(Level.Error, SafeFormat(message, objs));
        }

        public override void Error(Exception exception, string message, params object[] objs)
        {
            var text = SafeFormat(message, objs);

            Write(Level.Error, SafeFormat("{0} - exception: {1}", text, exception));
        }

        void Write(Level level, string message)
        {
            if ((int) level < (int) _minLevel) return;

            System.Console.WriteLine("{0:O}|{1}|{2}|{3}", DateTime.Now, level, _ownerType.FullName, message);
        }

        string SafeFormat(string message, params object[] objs)
        {
            try
            {
                return string.Format(message, objs);
            }
            catch (Exception)
            {
                return message;
            }
        }
 
    }
}