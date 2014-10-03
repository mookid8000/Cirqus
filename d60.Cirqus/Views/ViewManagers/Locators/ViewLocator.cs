using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers.Locators
{
    /// <summary>
    /// Abstracts away the logic of determining the scope of view instances by mapping from a <see cref="DomainEvent"/> to a view id
    /// </summary>
    public abstract class ViewLocator
    {
        static readonly ConcurrentDictionary<Type, ViewLocator> CachedViewLocators = new ConcurrentDictionary<Type, ViewLocator>();
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, bool>> CachedRelevancyChecks = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, bool>>();

        public static ViewLocator GetLocatorFor<TView>()
        {
            var viewType = typeof(TView);

            return CachedViewLocators.GetOrAdd(viewType, t => ActivateNewViewLocatorInstanceFromClosingType(viewType));
        }

        public static bool IsRelevant<TView>(DomainEvent domainEvent) where TView : ISubscribeTo
        {
            var domainEventType = domainEvent.GetType();
            var viewType = typeof(TView);
            
            return CachedRelevancyChecks
                .GetOrAdd(viewType, key => new ConcurrentDictionary<Type, bool>())
                .GetOrAdd(domainEventType, key => DetermineIfRelevant(domainEventType, viewType));
        }

        static bool DetermineIfRelevant(Type domainEventType, Type viewType)
        {
            var assignableTypes = GetAssignableTypes(domainEventType);
            var subscriberTypes = assignableTypes
                .Select(assignableType => typeof (ISubscribeTo<>).MakeGenericType(assignableType))
                .ToList();

            return subscriberTypes.Any(subscriberType => subscriberType.IsAssignableFrom(viewType));
        }

        static IEnumerable<Type> GetAssignableTypes(Type type)
        {
            var types = new[] {type}.ToList();

            var baseType = type.BaseType;
            while (baseType != null && typeof(DomainEvent).IsAssignableFrom(baseType))
            {
                types.Add(baseType);
                baseType = baseType.BaseType;
            }

            return types;
        }

        /// <summary>
        /// Gets the ID of the view to which this particular <see cref="DomainEvent"/> must be applied
        /// </summary>
        protected abstract IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e);

        /// <summary>
        /// Gets the IDs of the views to which the domain event must be dispatchers
        /// </summary>
        public IEnumerable<string> GetAffectedViewIds(IViewContext context, DomainEvent e)
        {
            context.CurrentEvent = e;

            return GetViewIds(context, e);
        } 

        /// <summary>
        /// Looks at the type closing the implemented <see cref="IViewInstance{TViewLocator}"/> and returns a new instance of that type
        /// </summary>
        static ViewLocator ActivateNewViewLocatorInstanceFromClosingType(Type viewType)
        {
            var genericViewType = viewType
                .GetInterfaces()
                .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IViewInstance<>));

            if (genericViewType == null)
            {
                throw new ArgumentException(
                    string.Format("Could not construct view dispatcher because the given" +
                                  " view type {0} is not an implementation of IViewInstance<> (i.e." +
                                  " you must implement IViewInstance<TViewLocator> so that it can be" +
                                  " figured out how to locate a specific view instance of" +
                                  " this type for each domain event)", viewType));
            }

            var viewLocatorType = genericViewType.GetGenericArguments()[0];

            try
            {
                return (ViewLocator)Activator.CreateInstance(viewLocatorType);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(
                    string.Format("Could not construct a view locator of type {0}", viewLocatorType), exception);
            }
        }
    }
}