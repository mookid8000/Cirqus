using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using d60.Cirqus.Events;

namespace d60.Cirqus.Projections.Views.ViewManagers
{
    public class HandlerViewLocator : ViewLocator
    {
        readonly ConcurrentDictionary<Type, MethodInfo> _handlerMethodsByDomainEventType = new ConcurrentDictionary<Type, MethodInfo>();

        protected sealed override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            var handlerMethod = _handlerMethodsByDomainEventType.GetOrAdd(e.GetType(), GetHandlerMethodFor);

            if (handlerMethod == null) return new string[0];

            try
            {
                var ids = ((IEnumerable<string>)handlerMethod.Invoke(this, new object[] { context, e })).ToList();

                Console.WriteLine("Dispatching {0} => {1}", e, string.Join(", ", ids));

                return ids;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not get IDs for domain event {0}!", e), exception);
            }
        }

        MethodInfo GetHandlerMethodFor(Type domainEventType)
        {
            var typesToCheck = new[] { domainEventType }
                .Concat(domainEventType.GetInterfaces())
                .Concat(GetBaseTypes(domainEventType));

            return typesToCheck
                .Select(typeToCheck => new[] { typeof(IViewContext), typeToCheck })
                .Select(parameterTypes => GetType().GetMethod("GetViewIds", parameterTypes))
                .FirstOrDefault(method => method != null);
        }

        IEnumerable<Type> GetBaseTypes(Type type)
        {
            return type.BaseType == null
                ? new Type[0]
                : new[] { type.BaseType }.Concat(GetBaseTypes(type.BaseType));
        }
    }
}