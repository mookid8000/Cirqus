using System;

namespace d60.Cirqus.Config
{
    /// <summary>
    /// Service that is responsible to mapping back and forth between aggregate root and domain event types and their names
    /// </summary>
    public interface IDomainTypeNameMapper
    {
        /// <summary>
        /// Gets the type from the name. Throws an exception if the given name could not be mapped unambiguously to tye type of either an aggregate root or a doman event
        /// </summary>
        Type GetType(string name);

        /// <summary>
        /// Gets the name from the type. Throws an exception if the given name could not be mapped in a way that can unambiguously be mapped back again
        /// </summary>
        string GetName(Type type);
    }
}