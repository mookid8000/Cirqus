using System;
using System.Runtime.Serialization;

namespace d60.Cirqus.Exceptions
{
    /// <summary>
    /// Exception that may be raised an inconsistency has been detected
    /// </summary>
    public class ConsistencyException : ApplicationException
    {
        public ConsistencyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ConsistencyException(string message, params object[] objs)
            : base(string.Format(message, objs))
        {
        }
    }
}