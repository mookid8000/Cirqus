using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    /// <summary>
    /// Base interface of views
    /// </summary>
    public interface IView
    {
        string Id { get; set; }
    }

    // ReSharper disable UnusedTypeParameter
    /// <summary>
    /// Base interface of a view that can be located - i.e., given some <see cref="DomainEvent"/>,
    /// it can be determined which view instance that must be updated with the event.
    /// </summary>
    /// <typeparam name="TViewLocator">The type of view locator that will be used to determine the ID of the view to be updated</typeparam>
    public interface IView<TViewLocator> : IView where TViewLocator : ViewLocator
    {
    }
    // ReSharper restore UnusedTypeParameter
}