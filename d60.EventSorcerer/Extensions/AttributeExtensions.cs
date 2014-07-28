using System;
using System.Linq;
using System.Reflection;

namespace d60.EventSorcerer.Extensions
{
    public static class AttributeExtensions
    {
        public static TAttribute GetAttributeOrDefault<TAttribute>(this ICustomAttributeProvider provider) where TAttribute : Attribute
        {
            return provider
                .GetCustomAttributes(typeof(TAttribute), true)
                .OfType<TAttribute>()
                .FirstOrDefault();
        }
        public static TReturn GetFromAttributeOrDefault<TAttribute, TReturn>(this ICustomAttributeProvider provider, Func<TAttribute, TReturn> func, TReturn defaultValue) where TAttribute : Attribute
        {
            var attribute = provider.GetAttributeOrDefault<TAttribute>();

            return attribute == null ? defaultValue : func(attribute);
        }
    }
}