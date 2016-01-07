using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.MongoDb.Views
{
    /// <summary>
    /// Optional marker interface that can be applied to a <see cref="IViewInstance"/> class if the view instance should be updated
    /// by setting <see cref="Failed"/> to true in the event of a failure within one particular view instance.
    /// Not all view managers are capable of supporting this feature.
    /// </summary>
    public interface ICanFailIndividually
    {
        bool Failed { get; set; } 
    }
}