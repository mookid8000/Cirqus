using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Base interface of views
    /// </summary>
    public interface IViewInstance
    {
        string Id { get; set; }
        long LastGlobalSequenceNumber { get; set; }
    }

    // ReSharper disable UnusedTypeParameter
    /// <summary>
    /// Base interface of a view that can be located - i.e., given some <see cref="DomainEvent"/>,
    /// it can be determined which view instance that must be updated with the event.
    /// </summary>
    /// <typeparam name="TViewLocator">The type of view locator that will be used to determine the ID of the view to be updated</typeparam>
    public interface IViewInstance<TViewLocator> : IViewInstance where TViewLocator : ViewLocator
    {
    }
    // ReSharper restore UnusedTypeParameter
}