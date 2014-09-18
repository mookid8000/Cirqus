using System.Diagnostics.Eventing.Reader;
using d60.Cirqus.Config;
using d60.Cirqus.Dispatch;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Config;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Config
{
    [TestFixture]
    public class TestEventDispatcherConfigurationApi : FixtureBase
    {
        [Test, Category(TestCategories.MongoDb)]
        public void YeahItWorks()
        {
            var database = MongoHelper.InitializeTestDatabase();

            var v11 = new InMemoryViewManager<View1>();
            var v12 = new InMemoryViewManager<View1>();
            var v21 = new InMemoryViewManager<View2>();
            var v22 = new InMemoryViewManager<View2>();

            var config = EventDispatcher.With()
                .ViewManager(v11, v21)
                .ViewManager(v12, v22);

            CommandProcessor.With()
                .EventStore(e => e.UseMongoDb(database, "Events"));
        }

        class Event : DomainEvent
        {
        }

        class View1 : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int Events { get; set; }
            public void Handle(IViewContext context, Event domainEvent)
            {
                Events++;
            }
        }

        class View2 : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<Event>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public int Events { get; set; }
            public void Handle(IViewContext context, Event domainEvent)
            {
                Events++;
            }
        }
    }
}