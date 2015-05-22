using System;

namespace d60.Cirqus.Logging
{
    /// <summary>
    /// Cirqus loggers are derived from this class
    /// </summary>
    public abstract class Logger
    {
        /// <summary>
        /// Defines the available log levels
        /// </summary>
        public enum Level
        {
            /// <summary>
            /// Debug logging is used for fairly detailed and verbose logging, like e.g. logging
            /// what's going on inside loops, etc. You will often turn OFF debug logging in production
            /// environments.
            /// </summary>
            Debug, 
            
            /// <summary>
            /// Info logging is used for useful information, not too verbose, and is meant to be
            /// turned on also for production environments.
            /// </summary>
            Info, 
            
            /// <summary>
            /// Warn logging is used to notify you of things that could potentially be an error, like
            /// e.g. an exception while trying to load events to dispatch to views, etc. It will be used
            /// in cases where the system can recover if the error is transient in nature.
            /// </summary>
            Warn, 
            
            /// <summary>
            /// Error logging is used in rare cases where some really unexpected condition has happened,
            /// like e.g. a background timer fired and wanted to trim an in-mem cache but then an
            /// exception occurred (which will most likely be some kind of logic error).
            /// </summary>
            Error
        }

        /// <summary>
        /// Logs the message with the <see cref="Level.Debug"/> level
        /// </summary>
        public abstract void Debug(string message, params object[] objs);

        /// <summary>
        /// Logs the message with the <see cref="Level.Info"/> level
        /// </summary>
        public abstract void Info(string message, params object[] objs);

        /// <summary>
        /// Logs the message with the <see cref="Level.Warn"/> level
        /// </summary>
        public abstract void Warn(string message, params object[] objs);

        /// <summary>
        /// Logs the message with the <see cref="Level.Warn"/> level, enclosing the given <paramref name="exception"/>
        /// </summary>
        public abstract void Warn(Exception exception, string message, params object[] objs);

        /// <summary>
        /// Logs the message with the <see cref="Level.Error"/> level
        /// </summary>
        public abstract void Error(string message, params object[] objs);

        /// <summary>
        /// Logs the message with the <see cref="Level.Error"/> level, enclosing the given <paramref name="exception"/>
        /// </summary>
        public abstract void Error(Exception exception, string message, params object[] objs);
    }
}