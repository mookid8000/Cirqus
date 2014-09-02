using System;

namespace d60.Cirqus.Logging
{
    public abstract class Logger
    {
        public enum Level
        {
            Debug, Info, Warn, Error
        }

        public abstract void Debug(string message, params object[] objs);
        
        public abstract void Info(string message, params object[] objs);
        
        public abstract void Warn(string message, params object[] objs);
        public abstract void Warn(Exception exception, string message, params object[] objs);
        
        public abstract void Error(string message, params object[] objs);
        public abstract void Error(Exception exception, string message, params object[] objs);
    }
}