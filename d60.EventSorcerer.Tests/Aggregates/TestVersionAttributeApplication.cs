using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Numbers;
using NUnit.Framework;
using TestContext = d60.EventSorcerer.TestHelpers.TestContext;

namespace d60.EventSorcerer.Tests.Aggregates
{
    [TestFixture]
    public class TestVersionAttributeApplication : FixtureBase
    {
        const int EmittedVersion = 4;
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = new TestContext();
        }

        [Test]
        public void AggregateVersionIsAppliedToEmittedEvents()
        {
            // arrange
            var root = _context.Get<Root>(Guid.NewGuid());

            // act
            root.DoStuff();

            // assert
            var emittedEvent = _context.UnitOfWork.Cast<Event>().Single();
            Console.WriteLine(emittedEvent.Meta);

            Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.RootVersion], Is.EqualTo(EmittedVersion));
            Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.EventVersion], Is.EqualTo(EmittedVersion));
            Assert.That(emittedEvent.Meta["whatever on the root"], Is.EqualTo("yo!"));
            Assert.That(emittedEvent.Meta["whatever on the event"], Is.EqualTo("yo again!"));
        }

        [Meta(DomainEvent.MetadataKeys.RootVersion, EmittedVersion)]
        [Meta("whatever on the root", "yo!")]
        public class Root : AggregateRoot, IEmit<Event>
        {
            public void DoStuff()
            {
                Emit(new Event());
            }

            public void Apply(Event e)
            {
                
            }
        }

        [Meta(MetadataKeys.EventVersion, EmittedVersion)]
        [Meta("whatever on the event", "yo again!")]
        public class Event : DomainEvent<Root>
        {
        }
    }
}