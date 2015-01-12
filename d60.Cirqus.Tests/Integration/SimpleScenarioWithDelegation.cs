using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture, Description(@"Simulates the entire pipeline of event processing:

1. command comes in
2. command is mapped to an operation on an aggregate root
3. new unit of work is created
4. operation is invoked, events are collected
5. part of executing the operation consists of loading another aggregate root
    and calling an operation on that
5. collected events are submitted atomically to event store
    a) on duplicate key error: go back to (3)
6. await synchronous views
7. publish events for consumption by chaser views
")]
    public class SimpleScenarioWithDelegation : FixtureBase
    {
        ICommandProcessor _cirqus;
        Task<InMemoryEventStore> _eventStore;
        IAggregateRootRepository _aggregateRootRepository;

        protected override void DoSetUp()
        {
            _cirqus = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .AggregateRootRepository(r => r.Register(c =>
                {
                    _aggregateRootRepository = new DefaultAggregateRootRepository(c.Get<IEventStore>(), c.Get<IDomainEventSerializer>(), c.Get<IDomainTypeNameMapper>());
                    
                    return _aggregateRootRepository;
                }))
                .EventDispatcher(e => e.UseConsoleOutEventDispatcher())
                .Create();

            RegisterForDisposal(_cirqus);
        }

        [Test]
        public void RunEntirePipelineAndProbePrivatesForMultipleAggregates()
        {
            var initialState = GetAllRoots();

            _cirqus.ProcessCommand(new BearSomeChildrenCommand("rootid1", new[] { "child_id1", "child_id2" }));

            var afterBearingTwoChildren = GetAllRoots();

            _cirqus.ProcessCommand(new BearSomeChildrenCommand("child_id1", new[] { "grandchild_id1" }));

            var afterBearingGrandChild = GetAllRoots();

            Assert.That(initialState.Count, Is.EqualTo(0), "No events yet, expected there to be zero agg roots in the repo");

            Assert.That(afterBearingTwoChildren.Count, Is.EqualTo(3));

            var idsOfChildren = afterBearingTwoChildren
                .OfType<ProgrammerAggregate>()
                .Single(p => p.Id == "rootid1")
                .GetIdsOfChildren();

            Assert.That(idsOfChildren, Is.EqualTo(new[] { "child_id1", "child_id2" }));

            Assert.That(afterBearingGrandChild.Count, Is.EqualTo(4));

            var idsOfGrandChildren = afterBearingGrandChild
                .OfType<ProgrammerAggregate>()
                .Single(p => p.Id == "child_id1")
                .GetIdsOfChildren();

            Assert.That(idsOfGrandChildren, Is.EqualTo(new[] { "grandchild_id1" }));
        }

        List<AggregateRoot> GetAllRoots()
        {
            return _eventStore.Result.Select(e => e.GetAggregateRootId()).Distinct()
                .Select(aggregateRootId => _aggregateRootRepository.Get<ProgrammerAggregate>(aggregateRootId))
                .Cast<AggregateRoot>()
                .ToList();
        }

        public class BearSomeChildrenCommand : Command<ProgrammerAggregate>
        {
            public string[] IdsOfChildren { get; private set; }

            public BearSomeChildrenCommand(string aggregateRootId, params string[] idsOfChildren)
                : base(aggregateRootId)
            {
                IdsOfChildren = idsOfChildren;
            }

            public override void Execute(ProgrammerAggregate aggregateRoot)
            {
                aggregateRoot.BearChildren(IdsOfChildren);
            }
        }

        public class ProgrammerAggregate : AggregateRoot, IEmit<HadChildren>, IEmit<WasBorn>
        {
            readonly List<string> _idsOfChildren = new List<string>();

            public void BearChildren(IEnumerable<string> idsOfChildren)
            {
                foreach (var id in idsOfChildren)
                {
                    (TryLoad<ProgrammerAggregate>(id) ?? Create<ProgrammerAggregate>(id)).GiveBirth();
                }

                Emit(new HadChildren(idsOfChildren));
            }

            public void Apply(WasBorn e)
            {

            }

            public void Apply(HadChildren e)
            {
                _idsOfChildren.AddRange(e.ChildrenIds);
            }

            void GiveBirth()
            {
                Emit(new WasBorn());
            }

            public IEnumerable<string> GetIdsOfChildren()
            {
                return _idsOfChildren;
            }
        }

        public class HadChildren : DomainEvent<ProgrammerAggregate>
        {
            public HadChildren(IEnumerable<string> childrenIds)
            {
                ChildrenIds = childrenIds.ToArray();
            }

            public string[] ChildrenIds { get; private set; }
        }

        public class WasBorn : DomainEvent<ProgrammerAggregate>
        {

        }
    }
}