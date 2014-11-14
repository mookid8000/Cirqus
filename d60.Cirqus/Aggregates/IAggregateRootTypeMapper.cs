using System;

namespace d60.Cirqus.Aggregates
{
    public interface IAggregateRootTypeMapper
    {
        Type GetType(string name);
        string GetName(Type type);
    }
}