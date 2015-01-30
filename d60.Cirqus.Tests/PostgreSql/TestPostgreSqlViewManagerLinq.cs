using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.PostgreSql.Views;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.PostgreSql
{
    [TestFixture, Category(TestCategories.PostgreSql)]
    public class TestPostgreSqlViewManagerLinq : FixtureBase
    {
        [Test]
        public void CanFindById()
        {
            var viewManager = new PostgreSqlViewManager<SimpleView>(PostgreSqlTestHelper.PostgreSqlConnectionString, "simpleView");

            viewManager.Dispatch(new ThrowingViewContext(), new DomainEvent[] { CreateEvent("hej") });

            var id = GlobalInstanceLocator.GetViewInstanceId();

            var instance = viewManager.AsQueryable()
                .First(i => i.Id == id);

            Assert.That(instance.Text, Is.EqualTo("hej"));
        }

        static long _globalSequenceNumber;

        static AnEvent CreateEvent(string text)
        {
            return new AnEvent
            {
                Meta = { { DomainEvent.MetadataKeys.GlobalSequenceNumber, (_globalSequenceNumber++).ToString() } },
                Text = text
            };
        }

        class AnEvent : DomainEvent
        {
            public string Text { get; set; }
        }

        class SimpleView : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<AnEvent>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public string Text { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                Text = domainEvent.Text;
            }
        }
    }
}