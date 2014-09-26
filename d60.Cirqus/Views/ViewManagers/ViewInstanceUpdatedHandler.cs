namespace d60.Cirqus.Views.ViewManagers
{
    public delegate void ViewInstanceUpdatedHandler<TViewInstance>(TViewInstance instance) where TViewInstance : IViewInstance;
}