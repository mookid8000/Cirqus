using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models
{
    public class EventEmitter : AggregateRoot, IEmit<AnEvent>
    {
        public void Apply(AnEvent e)
        {
        }

        public void DoIt()
        {
            Emit(new AnEvent());
        }
    }

    public class AnEvent : DomainEvent<EventEmitter>
    {
    }

    public class EmitEvent : Command<EventEmitter>
    {
        public EmitEvent(Guid aggregateRootId) : base(aggregateRootId)
        {
        }

        public override void Execute(EventEmitter aggregateRoot)
        {
            aggregateRoot.DoIt();
        }
    }

    public class HeaderCounter : IViewInstance<HeaderCounterViewLocator>, ISubscribeTo<AnEvent>
    {
        public HeaderCounter()
        {
            HeaderValues = new HashSet<string>();
        }

        public string Id { get; set; }
        
        public long LastGlobalSequenceNumber { get; set; }
        
        public HashSet<string> HeaderValues { get; set; }
        
        public void Handle(IViewContext context, AnEvent domainEvent)
        {
            var value = domainEvent.Meta[Id].ToString();

            HeaderValues.Add(value);
        }
    }

    public class HeaderCounterViewLocator : ViewLocator
    {
        protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
        {
            return e.Meta.Keys.ToArray();
        }
    }
}