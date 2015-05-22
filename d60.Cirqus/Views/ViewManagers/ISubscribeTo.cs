using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Base interface of something that subscribes to a domain event. This one is not meant to be implemented (because it doesn't really do anything)
    /// </summary>
    public interface ISubscribeTo { }

    /// <summary>
    /// Interface to implement by <see cref="IViewInstance"/>s in order to subscribe to different domain events
    /// </summary>
    public interface ISubscribeTo<in TDomainEvent> : ISubscribeTo where TDomainEvent : DomainEvent
    {
        /// <summary>
        /// Implement this method in order to have domain events dispatched to it
        /// </summary>
        void Handle(IViewContext context, TDomainEvent domainEvent);
    }
}