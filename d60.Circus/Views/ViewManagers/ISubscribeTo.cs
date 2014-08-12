using d60.Circus.Events;

namespace d60.Circus.Views.ViewManagers
{
    public interface ISubscribeTo { }
    public interface ISubscribeTo<TDomainEvent> : ISubscribeTo where TDomainEvent : DomainEvent
    {
        void Handle(IViewContext context, TDomainEvent domainEvent);
    }
}