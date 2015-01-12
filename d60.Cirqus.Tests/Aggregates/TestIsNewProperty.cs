using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestIsNewProperty : FixtureBase
    {
        TestContext _context;

        protected override void DoSetUp()
        {
            _context = RegisterForDisposal(TestContext.Create());
        }

        [Test]
        public void CanSeeThatAggregateRootIsNew()
        {
            const string aggregateRootId = "someId";

            _context.ProcessCommand(new InvokeBam(aggregateRootId));
            _context.ProcessCommand(new InvokeBam(aggregateRootId));
            _context.ProcessCommand(new InvokeBam(aggregateRootId));

            var instance = _context.AggregateRoots.OfType<Root>().Single(r => r.Id == aggregateRootId);
            Assert.That(instance.CollectedValuesOfIsNew, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void CanSeeThatDelegatedAggregateRootIsNew()
        {
            const string friendId = "friendId";

            _context.ProcessCommand(new InvokeBamOnFriend("someId") { FriendId = friendId });
            _context.ProcessCommand(new InvokeBamOnFriend("someId") { FriendId = friendId });
            _context.ProcessCommand(new InvokeBamOnFriend("someId") { FriendId = friendId });

            var instance = _context.AggregateRoots.OfType<Root>().Single(r => r.Id == friendId);
            Assert.That(instance.CollectedValuesOfIsNew, Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        [Description("In this case, the brand new aggregate root emits the OtherRootCreated event from its Created method - question is whether the root is to be considered new...")]
        public void AggregateRootIsNeverNewWhenEmittingOnCreation()
        {
            const string aggregateRootId = "someId";

            _context.ProcessCommand(new InvokeBamOnOtherRoot(aggregateRootId));
            _context.ProcessCommand(new InvokeBamOnOtherRoot(aggregateRootId));
            _context.ProcessCommand(new InvokeBamOnOtherRoot(aggregateRootId));

            var instance = _context.AggregateRoots.OfType<OtherRoot>().Single(r => r.Id == aggregateRootId);
            
            Assert.That(instance.CollectedValuesOfIsNew, Is.EqualTo(new[] { false, false, false }));
        }


        public class Root : AggregateRoot, IEmit<RootEvent>
        {
            readonly List<bool> _collectedValuesOfIsNew = new List<bool>();

            public List<bool> CollectedValuesOfIsNew
            {
                get { return _collectedValuesOfIsNew; }
            }

            public void Apply(RootEvent e)
            {
                _collectedValuesOfIsNew.Add(e.EmittedByNewAggregateRoot);
            }

            public void Bam()
            {
                Emit(new RootEvent
                {
                    EmittedByNewAggregateRoot = IsNew
                });
            }

            public void BamOnFriend(string friendId)
            {
                (TryLoad<Root>(friendId) ?? Create<Root>(friendId)).Bam();
            }

            public void BamOnOtherRoot(string otherRootId)
            {
                var otherRoot = TryLoad<OtherRoot>(otherRootId) ?? Create<OtherRoot>(otherRootId);
            }
        }

        public class OtherRoot : AggregateRoot, IEmit<OtherRootCreated>, IEmit<OtherRootEvent>
        {
            readonly List<bool> _collectedValuesOfIsNew = new List<bool>();

            protected override void Created()
            {
                Emit(new OtherRootCreated());
            }

            public void Bam()
            {
                Emit(new OtherRootEvent {EmittedByNewAggregateRoot = IsNew});
            }

            public void Apply(OtherRootCreated e)
            {
            }

            public void Apply(OtherRootEvent e)
            {
                _collectedValuesOfIsNew.Add(e.EmittedByNewAggregateRoot);
            }

            public List<bool> CollectedValuesOfIsNew
            {
                get { return _collectedValuesOfIsNew; }
            }
        }

        public class OtherRootCreated : DomainEvent<OtherRoot>
        {
        }

        public class OtherRootEvent : DomainEvent<OtherRoot>
        {
            public bool EmittedByNewAggregateRoot { get; set; }
        }

        public class RootEvent : DomainEvent<Root>
        {
            public bool EmittedByNewAggregateRoot { get; set; }
        }

        public class InvokeBam : Command<Root>
        {
            public InvokeBam(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Bam();
            }
        }

        public class InvokeBamOnFriend : Command<Root>
        {
            public InvokeBamOnFriend(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public string FriendId { get; set; }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.BamOnFriend(FriendId);
            }
        }

        public class InvokeBamOnOtherRoot : Command<OtherRoot>
        {
            public InvokeBamOnOtherRoot(string aggregateRootId) : base(aggregateRootId)
            {
            }

            public override void Execute(OtherRoot aggregateRoot)
            {
                aggregateRoot.Bam();
            }
        }
    }
}