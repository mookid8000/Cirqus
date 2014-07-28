using System;
using System.Collections.Concurrent;
using System.Linq;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public abstract class ViewLocator
    {
        static readonly ConcurrentDictionary<Type, ViewLocator> CachedViewLocators = new ConcurrentDictionary<Type, ViewLocator>();

        public static ViewLocator GetLocatorFor<TView>()
        {
            var viewType = typeof (TView);

            return CachedViewLocators
                .GetOrAdd(viewType, t =>
                {
                    var genericViewType = viewType
                        .GetInterfaces()
                        .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IView<>));

                    if (genericViewType == null)
                    {
                        throw new ArgumentException(
                            string.Format("Could not construct view dispatcher because the given" +
                                          " view type {0} is not an implementation of IView<> (i.e." +
                                          " you must implement IView<TViewLocator> so that it can be" +
                                          " figured out how to locate a specific view instance of" +
                                          " this type for each domain event)", viewType));
                    }

                    var viewLocatorType = genericViewType.GetGenericArguments()[0];

                    try
                    {
                        return (ViewLocator) Activator.CreateInstance(viewLocatorType);
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException(
                            string.Format("Could not construct a view locator of type {0}", viewLocatorType), exception);
                    }
                });
        }

        public abstract string GetViewId(DomainEvent e);
    }
}