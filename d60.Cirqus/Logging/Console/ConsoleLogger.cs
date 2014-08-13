using System;

namespace d60.Cirqus.Logging.Console
{
    public class ConsoleLogger : Logger
    {
        readonly Type _ownerType;
        readonly Level _minLevel;

        public ConsoleLogger(Type ownerType, Level minLevel = Level.Info)
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

        public override void Error(string message, params object[] objs)
        {
            Write(Level.Error, SafeFormat(message, objs));
        }

        void Write(Level level, string message)
        {
            if ((int) level < (int) _minLevel) return;

            System.Console.WriteLine("{0:O}|{1}|{2}|{3}", DateTime.Now, level, _ownerType.FullName, message);
        }

        string SafeFormat(string message, object[] objs)
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