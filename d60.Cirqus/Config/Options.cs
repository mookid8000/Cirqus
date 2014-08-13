using System;
using System.Collections.Generic;

namespace d60.Cirqus.Config
{
    public class Options
    {
        public const int DefaultMaxRetries = 10;

        readonly HashSet<Type> _domainExceptionTypes = new HashSet<Type>();

        public Options()
        {
            PurgeExistingViews = false;
            MaxRetries = DefaultMaxRetries;
        }

        /// <summary>
        /// Configures whether to purge all existing views during initialization.
        /// </summary>
        public bool PurgeExistingViews { get; set; }

        /// <summary>
        /// Configures the number of retries when processing commands.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Gets the registered domain exception types
        /// </summary>
        public IEnumerable<Type> DomainExceptionTypes { get { return _domainExceptionTypes; } }

        /// <summary>
        /// Registers the given exception type as a "domain exception", meaning that it will be passed
        /// directly to the caller of <seealso cref="CommandProcessor.ProcessCommand"/>
        /// </summary>
        public void AddDomainException<TException>() where TException : Exception
        {
            _domainExceptionTypes.Add(typeof (TException));
        }
    }
}