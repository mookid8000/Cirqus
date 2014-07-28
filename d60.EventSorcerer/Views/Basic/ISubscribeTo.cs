using d60.EventSorcerer.Events;

namespace d60.EventSorcerer.Views.Basic
{
    public interface ISubscribeTo<TDomainEvent> where TDomainEvent : DomainEvent
    {
        void Handle(TDomainEvent domainEvent);
    }
}