using System;

namespace d60.Cirqus.Aggregates
{
    public class DefaultAggregateRootTypeMapper : IAggregateRootTypeMapper
    {
        public Type GetType(string name)
        {
            var type = Type.GetType(name);

            if (type == null)
            {
                throw new ArgumentException(string.Format("Could not get aggregate root type from '{0}'", name));
            }

            return type;
        }

        public string GetName(Type type)
        {
            return string.Format("{0}, {1}", type.FullName, type.Assembly.GetName().Name);
        }
    }
}