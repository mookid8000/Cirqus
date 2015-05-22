using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestInheritance : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void ConcreteTypeIsAppliedAsOwnerToEmittedEvents()
        {
            // arrange
            using (var uow = _context.BeginUnitOfWork())
            {
                var root = uow.Load<EvenMoreExtendedAggregate>("someid");

                // act
                root.DoMore();
                root.DoSomething();

                // assert
                var emittedEvent = uow.EmittedEvents.OfType<SomeEvent>().First();
                Console.WriteLine(emittedEvent.Meta);

                Assert.That(uow.EmittedEvents.Count(), Is.EqualTo(2));
                Assert.That(emittedEvent.Meta[DomainEvent.MetadataKeys.Owner], Is.StringContaining(typeof(EvenMoreExtendedAggregate).Name));
            }
        }


        class SomeEvent : DomainEvent<SomeAggregate>
        {
            public readonly string What;

            public SomeEvent(string what)
            {
                What = what;
            }
        }

        abstract class SomeAggregate : AggregateRoot, IEmit<SomeEvent>
        {
            public readonly IList<string> StuffThatWasDone = new List<string>();

            public virtual void DoSomething()
            {
                Emit(new SomeEvent("emitted an event"));
            }

            public void Apply(SomeEvent e)
            {
                StuffThatWasDone.Add(e.What);
            }
        }

        class ExtendedAggregate : SomeAggregate
        {
            public void DoMore()
            {
                Emit(new SomeEvent("emitted an event"));
            }
        }

        class EvenMoreExtendedAggregate : ExtendedAggregate
        {
            public override void DoSomething()
            {
                base.DoSomething();
            }
        }
    }
}