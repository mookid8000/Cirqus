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
            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void YesWeCan()
        {
            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Load<Root>("id1");
                uow.Commit();
            }

            using (var uow = _context.BeginUnitOfWork())
            {
                uow.Load<Root>("id2");
                uow.Commit();
            }

            Assert.DoesNotThrow(() => _context.BeginUnitOfWork().Load<Root>("id1").AssociateWith("id2"));
        }


        public class Root : AggregateRoot, IEmit<RootCreated>, IEmit<RootAssociatedWithAnotherRoot>
        {
            readonly HashSet<string> _associatedOtherRootIds = new HashSet<string>();

            protected override void Created()
            {
                Emit(new RootCreated());
            }

            public void AssociateWith(string otherRootId)
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
            public string OtherRootId { get; set; }
        }

        public class RootCreated : DomainEvent<Root> { }
    }
}