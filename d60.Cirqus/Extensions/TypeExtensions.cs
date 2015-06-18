using System;
using System.Linq;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Nifty extensions for <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets a much-improved name of the type, optionally including namespace information
        /// </summary>
        public static string GetPrettyName(this Type type, bool includeNamespace = false)
        {
            return GetTypeName(type, includeNamespace);
        }

        static string GetTypeName(Type type, bool includeNamespace)
        {
            var typeName = includeNamespace ? type.FullName : type.Name;

            if (typeName.Contains('`'))
            {
                typeName = typeName.Substring(0, typeName.IndexOf('`'));
            }
            
            if (type.IsGenericType)
            {
                var typeArgumentsString = string.Join(",", type.GetGenericArguments()
                    .Select(typeArgument => GetTypeName(typeArgument, includeNamespace)));

                return string.Format("{0}<{1}>", typeName, typeArgumentsString);
            }

            return typeName;
        }
    }
}