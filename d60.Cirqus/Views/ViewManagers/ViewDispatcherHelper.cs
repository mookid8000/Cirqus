using System;
using System.Collections.Concurrent;
using System.Reflection;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Helper that can dispatch events to an instance of a class that implements any number of
    /// <see cref="ISubscribeTo{TDomainEvent}"/> interfaces
    /// </summary>
    public class ViewDispatcherHelper<TViewInstance> where TViewInstance : ISubscribeTo, IViewInstance, new()
    {
        readonly ConcurrentDictionary<Type, MethodInfo> _dispatcherMethods = new ConcurrentDictionary<Type, MethodInfo>();
        readonly Logger _logger = CirqusLoggerFactory.Current.GetCurrentClassLogger();
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

        public void DispatchToView(IViewContext context, DomainEvent domainEvent, TViewInstance view)
        {
            var lastGlobalSequenceNumber = domainEvent.GetGlobalSequenceNumber();

            if (lastGlobalSequenceNumber <= view.LastGlobalSequenceNumber) return;

            var domainEventType = domainEvent.GetType();

            var dispatcherMethod = _dispatcherMethods
                .GetOrAdd(domainEventType, type => _dispatchToViewGenericMethod.MakeGenericMethod(domainEventType));

            try
            {
                _logger.Debug("Dispatching event {0} to {1} with ID {2}", lastGlobalSequenceNumber, view, view.Id);

                context.CurrentEvent = domainEvent;

                dispatcherMethod.Invoke(this, new object[] { context, domainEvent, view });

                view.LastGlobalSequenceNumber = lastGlobalSequenceNumber;
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not dispatch {0} to {1}", domainEvent, view), exception);
            }
        }

        public TViewInstance CreateNewInstance(string viewId)
        {
            return new TViewInstance
            {
                Id = viewId,
                LastGlobalSequenceNumber = -1
            };
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