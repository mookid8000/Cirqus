using System;
using d60.Cirqus.Extensions;
using d60.Cirqus.Projections.Views.ViewManagers;
using d60.Cirqus.Projections.Views.ViewManagers.Locators;
using d60.Cirqus.Tests.Projections.Views.NewViewManager.Events;

namespace d60.Cirqus.Tests.Projections.Views.NewViewManager.Views
{
    public class PotatoTimeToBeConsumedView : IViewInstance<InstancePerAggregateRootLocator>,
        ISubscribeTo<PotatoCreated>,
        ISubscribeTo<WasEaten>
    {
        public string Id { get; set; }

        public long LastGlobalSequenceNumber { get; set; }

        public string Name { get; set; }

        public DateTime TimeOfCreation { get; set; }

        public TimeSpan TimeToBeEaten { get; set; }

        public void Handle(IViewContext context, PotatoCreated domainEvent)
        {
            Name = domainEvent.Name;
            
            TimeOfCreation = domainEvent.GetUtcTime();
        }

        public void Handle(IViewContext context, WasEaten domainEvent)
        {
            Console.WriteLine("{2}: Setting time to be eaten - toc: {0} (kind: {1})", TimeOfCreation, TimeOfCreation.Kind, Name);
            var timeOfEating = domainEvent.GetUtcTime();

            TimeToBeEaten = timeOfEating - TimeOfCreation;
        }

        public override string ToString()
        {
            return string.Format("{0} / {1} / {2} / {3}", Id, Name, TimeOfCreation, TimeToBeEaten);
        }
    }
}