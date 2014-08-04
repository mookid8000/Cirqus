using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public interface ISubscribeTo { }
    public interface ISubscribeTo<TDomainEvent> : ISubscribeTo where TDomainEvent : DomainEvent
    {
        void Handle(IViewContext context, TDomainEvent domainEvent);
    }
}