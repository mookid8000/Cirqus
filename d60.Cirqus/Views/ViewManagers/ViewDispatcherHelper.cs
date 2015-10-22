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

        /// <summary>
        /// Constructs the dispatcher helper
        /// </summary>
        public ViewDispatcherHelper()
        {
            _dispatchToViewGenericMethod = GetType()
                .GetMethod("DispatchToViewGeneric", BindingFlags.NonPublic | BindingFlags.Instance);

            if (_dispatchToViewGenericMethod == null)
            {
                throw new ApplicationException("Could not find dispatcher method 'DispatchToViewGeneric<>' on InMemoryViewManager");
            }
        }

        /// <summary>
        /// Dispatches the given domain event to the view instance. The view instance's <see cref="IViewInstance.LastGlobalSequenceNumber"/>
        /// will automatically be updated after successful dispatch. If the global sequence number of the event is lower or equal to the
        /// view instance's <see cref="IViewInstance.LastGlobalSequenceNumber"/>, the event is ignored.
        /// </summary>
        public void DispatchToView(IViewContext context, DomainEvent domainEvent, TViewInstance view)
        {
            var lastGlobalSequenceNumber = domainEvent.GetGlobalSequenceNumber();

            if (lastGlobalSequenceNumber <= view.LastGlobalSequenceNumber) return;

            var domainEventType = domainEvent.GetType();

            var dispatcherMethod = _dispatcherMethods
                .GetOrAdd(domainEventType, type => _dispatchToViewGenericMethod.MakeGenericMethod(domainEventType));

            try
            {
                var viewId = view.Id;

                _logger.Debug("Dispatching event {0} to {1} with ID {2}", lastGlobalSequenceNumber, view, viewId);

                context.CurrentEvent = domainEvent;

                dispatcherMethod.Invoke(this, new object[] {context, domainEvent, view});

                view.Id = viewId;
                view.LastGlobalSequenceNumber = lastGlobalSequenceNumber;
            }
            catch (TargetInvocationException exception)
            {
                throw new ApplicationException(string.Format("Could not dispatch {0} to {1}", domainEvent, view), exception.InnerException);
            }
            catch (Exception exception)
            {
                throw new ApplicationException(string.Format("Could not dispatch {0} to {1}", domainEvent, view), exception);
            }
        }

        /// <summary>
        /// Creates a new view instance with the given <paramref name="viewId"/>
        /// </summary>
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