namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Typed API for a view manager that allows for addressing type-specific view managers from the outside of the dispatcher
    /// </summary>
    public interface IViewManager<TViewInstance> : IViewManager where TViewInstance : IViewInstance
    {
        /// <summary>
        /// Loads the view instance with the specified ID, returning null if it does not exist
        /// </summary>
        TViewInstance Load(string viewId);

        /// <summary>
        /// Deletes the view with the specified ID if it exists
        /// </summary>
        void Delete(string viewId);

        /// <summary>
        /// Event that is raised whenever a view instance is created/updated
        /// </summary>
        event ViewInstanceUpdatedHandler<TViewInstance> Updated;
    }
}