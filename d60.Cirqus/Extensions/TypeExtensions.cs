using System;
using System.Linq;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Extensions
{
    /// <summary>
    /// Nifty extensions for <see cref="Type"/>
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Gets the type for the class that implements <see cref="IViewInstance"/> if possible
        /// </summary>
        public static Type GetViewType(this IViewManager viewManager)
        {
            var type = viewManager.GetType();

            var implementedViewManagerInterfaces = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IViewManager<>))
                .ToList();

            if (implementedViewManagerInterfaces.Count == 0)
            {
                throw new InvalidOperationException(string.Format("Cannot get view type from view manager of type {0} because it does not implement IViewManager<TViewInstance>", type));
            }

            if (implementedViewManagerInterfaces.Count > 1)
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot unambiguously determine the view type for view manager of type {0} because it implements IViewManager<TViewInstance> for multiple types: {1}",
                        type, string.Join(", ", implementedViewManagerInterfaces.Select(i => i.GetPrettyName()))));
            }

            return implementedViewManagerInterfaces.Single().GetGenericArguments().First();
        }

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