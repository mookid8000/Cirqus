using System;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Views.Basic;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.Views
{
    [TestFixture]
    public class TestViewLocator : FixtureBase
    {
        [Test]
        public void DoesNotCallViewLocatorForIrrelevantEvents()
        {
            var manager = new InMemoryViewManager<MyView>();
            var testContext = new TestContext().AddViewManager(manager);

            testContext.Save(Guid.NewGuid(), new AnEvent());
            testContext.Save(Guid.NewGuid(), new AnotherEvent());

            Assert.DoesNotThrow(testContext.Commit);
        }

        class MyView : IView<CustomizedViewLocator>, ISubscribeTo<AnEvent>
        {
            public string Id { get; set; }
            public void Handle(IViewContext context, AnEvent domainEvent)
            {
                
            }
        }
        class CustomizedViewLocator : ViewLocator
        {
            public override string GetViewId(DomainEvent e)
            {
                if (e is AnEvent) return "yay";

                throw new ApplicationException("oh noes!!!!");
            }
        }

        class AnEvent : DomainEvent<Root>
        {
            
        }
        class AnotherEvent : DomainEvent<Root>
        {
            
        }

        class Root : AggregateRoot
        {
            
        }
    }
}