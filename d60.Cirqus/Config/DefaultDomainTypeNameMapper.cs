using System;

namespace d60.Cirqus.Config
{
    /// <summary>
    /// Implementation of <see cref="IDomainTypeNameMapper"/> that uses assembly-qualified type names without version and culture information
    /// </summary>
    public class DefaultDomainTypeNameMapper : IDomainTypeNameMapper
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