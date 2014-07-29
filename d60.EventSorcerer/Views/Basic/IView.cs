namespace d60.EventSorcerer.Views.Basic
{
    public interface IView { }

    public interface IView<TViewLocator> : IView where TViewLocator : ViewLocator
    {
    }
}