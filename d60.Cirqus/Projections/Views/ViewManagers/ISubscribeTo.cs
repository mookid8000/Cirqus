using d60.Cirqus.Events;

namespace d60.Cirqus.Projections.Views.ViewManagers
{
    public interface ISubscribeTo { }
    public interface ISubscribeTo<TDomainEvent> : ISubscribeTo where TDomainEvent : DomainEvent
    {
        void Handle(IViewContext context, TDomainEvent domainEvent);
    }
}