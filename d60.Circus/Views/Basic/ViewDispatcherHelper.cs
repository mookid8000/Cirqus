using System;
using System.Collections.Concurrent;
using System.Reflection;
using d60.Circus.Events;

namespace d60.Circus.Views.Basic
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

        public void DispatchToView(IViewContext context, DomainEvent domainEvent, TView view)
        {
            var domainEventType = domainEvent.GetType();

            var dispatcherMethod = _dispatcherMethods
                .GetOrAdd(domainEventType, type => _dispatchToViewGenericMethod.MakeGenericMethod(domainEventType));

            try
            {
                dispatcherMethod.Invoke(this, new object[] { context, domainEvent, view });
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not dispatch {0} to {1}", domainEvent, view), exception);
            }
        }

        // ReSharper disable UnusedMember.Local
        void DispatchToViewGeneric<TDomainEvent>(IViewContext context, TDomainEvent domainEvent, IViewInstance viewInstance) where TDomainEvent : DomainEvent
        {
            var handler = viewInstance as ISubscribeTo<TDomainEvent>;

            if (handler == null) return;

            handler.Handle(context, domainEvent);
        }
        // ReSharper restore UnusedMember.Local 
    }
}