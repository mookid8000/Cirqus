using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyThatOtherRootsCanBeLoadedAfterEmittingEvents : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void YesWeCan()
        {
            var root1Id = Guid.NewGuid();
            var root2Id = Guid.NewGuid();

            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(root1Id);
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Get<Root>(root2Id);
                uow.Commit();
            }

            Assert.DoesNotThrow(() => _context.BeginUnitOfWork().Get<Root>(root1Id).AssociateWith(root2Id));
        }


        public class Root : AggregateRoot, IEmit<RootCreated>, IEmit<RootAssociatedWithAnotherRoot>
        {
            readonly HashSet<Guid> _associatedOtherRootIds = new HashSet<Guid>();

            protected override void Created()
            {
                Emit(new RootCreated());
            }

            public void AssociateWith(Guid otherRootId)
            {
                Emit(new RootAssociatedWithAnotherRoot
                {
                    OtherRootId = otherRootId
                });

                var loadedOtherRoot = Load<Root>(otherRootId);

                Console.WriteLine("Loaded: {0}", loadedOtherRoot.Id);
            }

            public void Apply(RootCreated e)
            {
            }

            public void Apply(RootAssociatedWithAnotherRoot e)
            {
                _associatedOtherRootIds.Add(e.OtherRootId);
            }
        }

        public class RootAssociatedWithAnotherRoot : DomainEvent<Root>
        {
            public Guid OtherRootId { get; set; }
        }

        public class RootCreated : DomainEvent<Root> { }
    }
}