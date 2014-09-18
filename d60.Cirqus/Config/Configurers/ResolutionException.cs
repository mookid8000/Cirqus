using System;
using System.Runtime.Serialization;

namespace d60.Cirqus.Config.Configurers
{
    [Serializable]
    public class ResolutionException : ApplicationException
    {
        public ResolutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public ResolutionException(Type resolvedType, string message, params object[] objs)
            : base(CreateMessage(resolvedType, message, objs))
        {
        }

        public ResolutionException(Exception innerException, Type resolvedType, string message, params object[] objs)
            : base(CreateMessage(resolvedType, message, objs), innerException)
        {
        }

        static string CreateMessage(Type resolvedType, string message, object[] objs)
        {
            return string.Format("Error when getting {0}: {1}", resolvedType, string.Format(message, objs));
        }
    }
}