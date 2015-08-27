using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers.Locators;
// ReSharper disable UnusedTypeParameter

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Base interface of views - marks a class as a representation of one instance of a view model that can
    /// have events dispatched to it.
    /// </summary>
    public interface IViewInstance
    {
        /// <summary>
        /// Holds the ID of this particular view instance. The value will be managed from the outside, so you should never change this one
        /// </summary>
        string Id { get; set; }
        
        /// <summary>
        /// Holds the last global sequence number of the most recent <see cref="DomainEvent"/> that this view instance has handled. This
        /// is used to make each view instance idempotent, so that event dispatch to individual views ise guaranteed to happen exactly
        /// once
        /// </summary>
        long LastGlobalSequenceNumber { get; set; }
    }

    /// <summary>
    /// Base interface of a view that can be located - i.e., given some <see cref="DomainEvent"/>,
    /// it can be determined which view instance that must be updated with the event.
    /// </summary>
    /// <typeparam name="TViewLocator">The type of view locator that will be used to determine the ID of the view to be updated</typeparam>
    public interface IViewInstance<TViewLocator> : IViewInstance where TViewLocator : ViewLocator
    {
    }
}