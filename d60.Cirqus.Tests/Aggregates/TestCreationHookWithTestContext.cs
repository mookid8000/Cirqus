using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestCreationHookWithTestContext : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void InvokesCreatedHookWhenAggregateRootIsFirstCreated()
        {
            // arrange
            var rootId = Guid.NewGuid();

            // act
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(rootId).DoSomething();
                uow.Commit();
            }

            // assert
            var expectedSequenceOfEvents = new[] { typeof(RootCreated), typeof(RootDidSomething) };
            var actualSequenceOfEvents = _context.History.Select(e => e.GetType()).ToArray();

            Assert.That(actualSequenceOfEvents, Is.EqualTo(expectedSequenceOfEvents));
        }

        [Test]
        public void InvokesCreatedHookWhenAggregateRootIsFirstCreatedAndNeverAgain()
        {
            // arrange
            var rootId = Guid.NewGuid();

            // act
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(rootId).DoSomething();
                uow.Commit();
            }
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(rootId).DoSomething();
                uow.Commit();
            }
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(rootId).DoSomething();
                uow.Commit();
            }

            // assert
            var expectedSequenceOfEvents = new[]
            {
                typeof(RootCreated), 
                typeof(RootDidSomething), 
                typeof(RootDidSomething), 
                typeof(RootDidSomething)
            };
            var actualSequenceOfEvents = _context.History.Select(e => e.GetType()).ToArray();

            Assert.That(actualSequenceOfEvents, Is.EqualTo(expectedSequenceOfEvents));
        }


        public class Root : AggregateRoot,
            IEmit<RootCreated>,
            IEmit<RootDidSomething>
        {
            protected override void Created()
            {
                Emit(new RootCreated());
            }

            public void DoSomething()
            {
                Emit(new RootDidSomething());
            }

            public void Apply(RootCreated e)
            {

            }

            public void Apply(RootDidSomething e)
            {

            }
        }

        public class RootCreated : DomainEvent<Root> { }
        public class RootDidSomething : DomainEvent<Root> { }
    }
}