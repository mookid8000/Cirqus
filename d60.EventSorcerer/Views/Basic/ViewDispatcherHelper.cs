using System;
using System.Collections.Concurrent;
using System.Reflection;
using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    /// <summary>
    /// Helper that can dispatch events to an instance of a class that implements any number of
    /// <see cref="ISubscribeTo{TDomainEvent}"/> interfaces
    /// </summary>
    public class ViewDispatcherHelper<TView> where TView : ISubscribeTo
    {
        readonly ConcurrentDictionary<Type, MethodInfo> _dispatcherMethods = new ConcurrentDictionary<Type, MethodInfo>();
        readonly MethodInfo _dispatchToViewGenericMethod;

        public ViewDispatcherHelper()
        {
            _dispatchToViewGenericMethod = GetType()
                .GetMethod("DispatchToViewGeneric", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_dispatchToViewGenericMethod == null)
            {
                throw new ApplicationException("Could not find dispatcher method 'DispatchToViewGeneric<>' on InMemoryViewManager");
            }
        }

        public void DispatchToView(DomainEvent domainEvent, TView view)
        {
            var domainEventType = domainEvent.GetType();

            var dispatcherMethod = _dispatcherMethods
                .GetOrAdd(domainEventType, type => _dispatchToViewGenericMethod.MakeGenericMethod(domainEventType));

            try
            {
                dispatcherMethod.Invoke(this, new object[] { domainEvent, view });
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not dispatch {0} to {1}", domainEvent, view), exception);
            }
        }

        // ReSharper disable UnusedMember.Local
        void DispatchToViewGeneric<TDomainEvent>(TDomainEvent domainEvent, IView view) where TDomainEvent : DomainEvent
        {
            var handler = view as ISubscribeTo<TDomainEvent>;

            if (handler == null) return;

            handler.Handle(domainEvent);
        }
        // ReSharper restore UnusedMember.Local 
    }
}