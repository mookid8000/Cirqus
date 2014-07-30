namespace d60.EventSorcerer.Views.Basic
{
    public interface IView
    {
        string Id { get; set; } 
    }

    public interface IView<TViewLocator> : IView where TViewLocator : ViewLocator
    {
    }
}