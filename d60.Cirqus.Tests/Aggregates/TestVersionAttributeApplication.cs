using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestVersionAttributeApplication : FixtureBase
    {
        const string EmittedVersion = "4";
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void AggregateVersionIsAppliedToEmittedEvents()
        {
            // arrange
            var uow = _context.BeginUnitOfWork();
            var root = uow.Load<Root>("someid");

            // act
            root.DoStuff();

            // assert
            var emittedEvent = uow.EmittedEvents.OfType<Event>().Single();
            Console.WriteLine(emittedEvent.Meta);

            Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.RootVersion], Is.EqualTo(EmittedVersion));
            Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.EventVersion], Is.EqualTo(EmittedVersion));
            Assert.That(emittedEvent.Meta["whatever on the root"], Is.EqualTo("yo!"));
            Assert.That(emittedEvent.Meta["whatever on the event"], Is.EqualTo("yo again!"));
        }

        [Test]
        public void CustomAttributeIsAppliedAsWell()
        {
            // arrange
            var uow = _context.BeginUnitOfWork();
            var root = uow.Load<Root>("rootid");

            // act
            root.DoStuff();

            // assert
            var emittedEvent = uow.EmittedEvents.OfType<AnotherEvent>().Single();
            Console.WriteLine(emittedEvent.Meta);

            Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.EventVersion], Is.EqualTo("4"));
        }

        [Meta(DomainEvent.MetadataKeys.RootVersion, EmittedVersion)]
        [Meta("whatever on the root", "yo!")]
        public class Root : AggregateRoot, IEmit<Event>, IEmit<AnotherEvent>
        {
            public void DoStuff()
            {
                Emit(new Event());
                Emit(new AnotherEvent());
            }

            public void Apply(Event e)
            {

            }

            public void Apply(AnotherEvent e)
            {
                
            }
        }

        [Meta(MetadataKeys.EventVersion, EmittedVersion)]
        [Meta("whatever on the event", "yo again!")]
        public class Event : DomainEvent<Root>
        {
        }

        [EventVersion(4)]
        public class AnotherEvent : DomainEvent<Root>
        {
        }

        public class EventVersionAttribute : MetaAttribute
        {
            public EventVersionAttribute(int version)
                : base(DomainEvent.MetadataKeys.EventVersion, version.ToString())
            {

            }
        }
    }
}