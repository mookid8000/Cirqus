using System;

namespace d60.Cirqus.Logging.Debug
{
    /// <summary>
    /// An implementation of <see cref="Logger"/> that logs to Debug. Useful in web projects.
    /// </summary>
    public class DebugLogger : Logger
    {
        readonly Level _minLevel;
        readonly Type _ownerType;

        /// <summary>
        /// Constructs a new logger that logs to <see cref="System.Diagnostics.Debug.WriteLine(string)"/>
        /// </summary>
        public DebugLogger(Type ownerType, Level minLevel = Level.Debug)
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

            System.Diagnostics.Debug.WriteLine("{0:O}|{1}|{2}|{3}", DateTime.Now, level, _ownerType.FullName, message);
        }

        private string SafeFormat(string message, params object[] objs)
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