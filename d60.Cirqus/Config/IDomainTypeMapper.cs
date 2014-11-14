using System;

namespace d60.Cirqus.Config
{
    public interface IDomainTypeMapper
    {
        Type GetType(string name);
        string GetName(Type type);
    }
}