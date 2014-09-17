using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views.ViewManagers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestViewLocator : FixtureBase
    {
        [Test]
        public void CanGetViewIdsFromHandlerTypeViewLocatorWhenEventImplementsAnInterface()
        {
            // arrange
            var locator = ViewLocator.GetLocatorFor<ViewWithYourSpecialHandlerLikeViewLocatorWithInterface>();

            // act
            var ids = locator.GetAffectedViewIds(new ThrowingViewContext(), new AnotherEvent());

            // assert
            Assert.That(ids.ToArray(), Is.EqualTo(new[] { "interface, as expected!" }));
        }

        class ViewWithYourSpecialHandlerLikeViewLocatorWithInterface : IViewInstance<YourSpecialHandlerLikeViewLocatorWithInterface>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
        }

        class YourSpecialHandlerLikeViewLocatorWithInterface : HandlerViewLocator,
            IGetViewIdsFor<IAmAnEvent>
        {
            public IEnumerable<string> GetViewIds(IViewContext context, IAmAnEvent e)
            {
                yield return "interface, as expected!";
            }
        }
        [Test]
        public void CanGetViewIdsFromHandlerTypeViewLocatorWhenEventIsInherited()
        {
            // arrange
            var locator = ViewLocator.GetLocatorFor<ViewWithYourSpecialHandlerLikeViewLocatorWithInheritance>();

            // act
            var ids = locator.GetAffectedViewIds(new ThrowingViewContext(), new YetAnotherEvent());

            // assert
            Assert.That(ids.ToArray(), Is.EqualTo(new[] { "as expected!" }));
        }

        class ViewWithYourSpecialHandlerLikeViewLocatorWithInheritance : IViewInstance<YourSpecialHandlerLikeViewLocatorWithInheritance>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
        }

        class YourSpecialHandlerLikeViewLocatorWithInheritance : HandlerViewLocator,
            IGetViewIdsFor<AnotherEvent>
        {
            public IEnumerable<string> GetViewIds(IViewContext context, AnotherEvent e)
            {
                yield return "as expected!";
            }
        }

        [Test]
        public void CanGetViewIdsFromHandlerTypeViewLocator()
        {
            // arrange
            var locator = ViewLocator.GetLocatorFor<ViewWithYourSpecialHandlerLikeViewLocator>();

            // act
            var ids = locator.GetAffectedViewIds(new ThrowingViewContext(), new Event());

            // assert
            Assert.That(ids.ToArray(), Is.EqualTo(new[] { "some", "more", "known", "ids" }));
        }

        class ViewWithYourSpecialHandlerLikeViewLocator : IViewInstance<YourSpecialHandlerLikeViewLocator>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
        }

        class YourSpecialHandlerLikeViewLocator : HandlerViewLocator,
            IGetViewIdsFor<Event>
        {
            public IEnumerable<string> GetViewIds(IViewContext context, Event e)
            {
                yield return "some";
                yield return "more";
                yield return "known";
                yield return "ids";
            }
        }

        class AnotherEvent : DomainEvent, IAmAnEvent { }

        interface IAmAnEvent { }

        class YetAnotherEvent : AnotherEvent { }

        [Test]
        public void CanGetViewIdsFromImplementation()
        {
            // arrange
            var locator = ViewLocator.GetLocatorFor<ViewWithYourStandardViewLocator>();

            // act
            var viewIds = locator.GetAffectedViewIds(new ThrowingViewContext(), new Event());

            // assert
            Assert.That(viewIds.ToArray(), Is.EqualTo(new[] { "some", "known", "ids" }));
        }

        class Event : DomainEvent { }

        class ViewWithYourStandardViewLocator : IViewInstance<YourStandardViewLocator>
        {
            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
        }

        class YourStandardViewLocator : ViewLocator
        {
            protected override IEnumerable<string> GetViewIds(IViewContext context, DomainEvent e)
            {
                yield return "some";
                yield return "known";
                yield return "ids";
            }
        }

    }
}