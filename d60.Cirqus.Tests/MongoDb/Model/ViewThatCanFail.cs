using System;
using d60.Cirqus.Extensions;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.MongoDb.Model
{
    public class ViewThatCanFail : IViewInstance<InstancePerAggregateRootLocator>,
        ISubscribeTo<RootIncrementedTo>,
        ICanFailIndividually
    {
        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public bool Failed { get; set; }
        public int Number { get; set; }
        public void Handle(IViewContext context, RootIncrementedTo domainEvent)
        {
            Number = domainEvent.Number;

            // muahahahaha
            if (Number > 3 && domainEvent.GetAggregateRootId() == "id3")
            {
                throw new InvalidOperationException(@"




(╯°□°)╯︵ ┻━┻





");
            }
        }
    }
}