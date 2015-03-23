using System.Threading;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Contracts.Views.Models.ViewProfiling
{
    public class View : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<FirstEvent>, ISubscribeTo<SecondEvent>
    {
        public const int FirstEventSleepMilliseconds = 100;
        public const int SecondEventSleepMilliseconds = 400;

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        
        public void Handle(IViewContext context, FirstEvent domainEvent)
        {
            Thread.Sleep(FirstEventSleepMilliseconds);
        }

        public void Handle(IViewContext context, SecondEvent domainEvent)
        {
            Thread.Sleep(SecondEventSleepMilliseconds);
        }
    }

    public class Root : AggregateRoot { }
    public class FirstEvent : DomainEvent<Root> { }
    public class SecondEvent : DomainEvent<Root> { }
}