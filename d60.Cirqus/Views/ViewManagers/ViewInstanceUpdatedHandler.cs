namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Delegate type of an event handler that will be called when a view instance has been updated beucase one or more events were dispatched to it
    /// </summary>
    public delegate void ViewInstanceUpdatedHandler<TViewInstance>(TViewInstance instance) where TViewInstance : IViewInstance;
}