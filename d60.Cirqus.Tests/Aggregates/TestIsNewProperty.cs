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
            _context = RegisterForDisposal(new TestContext());
        }

        [Test]
        public void CanSeeThatAggregateRootIsNew()
        {
            const string aggregateRootId = "someId";

            _context.ProcessCommand(new InvokeBam(aggregateRootId));
            _context.ProcessCommand(new InvokeBam(aggregateRootId));
            _context.ProcessCommand(new InvokeBam(aggregateRootId));

            var instance = _context.AggregateRoots.OfType<Root>().Single(r => r.Id == aggregateRootId);
            Assert.That(instance.CollectedValuesOfIsNew, Is.EqualTo(new[]{true, false, false}));
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

        public class Root : AggregateRoot, IEmit<Event>
        {
            readonly List<bool> _collectedValuesOfIsNew = new List<bool>();

            public List<bool> CollectedValuesOfIsNew
            {
                get { return _collectedValuesOfIsNew; }
            }

            public void Apply(Event e)
            {
                _collectedValuesOfIsNew.Add(e.EmittedByNewAggregateRoot);
            }

            public void Bam()
            {
                Emit(new Event
                {
                    EmittedByNewAggregateRoot = IsNew
                });
            }

            public void BamOnFriend(string friendId)
            {
                Load<Root>(friendId, createIfNotExists: true).Bam();
            }
        }

        public class Event : DomainEvent<Root>
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
    }
}