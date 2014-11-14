using System;

namespace d60.Cirqus.Config
{
    /// <summary>
    /// Service that is responsible to mapping back and forth between aggregate root and domain event types and their names
    /// </summary>
    public interface IDomainTypeNameMapper
    {
        Type GetType(string name);
        string GetName(Type type);
    }
}