using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.TestHelpers.Internals;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.Stubs;
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
        CommandProcessor _cirqus;
        DefaultAggregateRootRepository _aggregateRootRepository;
        InMemoryEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();

            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);

            var viewManager = new ConsoleOutEventDispatcher();

            _cirqus = new CommandProcessor(_eventStore, _aggregateRootRepository, viewManager);
        }

        [Test]
        public void RunEntirePipelineAndProbePrivatesForMultipleAggregates()
        {
            var firstAggregateRootId = Guid.NewGuid();

            var firstChildId = Guid.NewGuid();
            var secondChildId = Guid.NewGuid();

            var grandChildId = Guid.NewGuid();

            var initialState = GetAllRoots();

            _cirqus.ProcessCommand(new BearSomeChildrenCommand(firstAggregateRootId, new[] { firstChildId, secondChildId }));

            var afterBearingTwoChildren = GetAllRoots();

            _cirqus.ProcessCommand(new BearSomeChildrenCommand(firstChildId, new[] { grandChildId }));

            var afterBearingGrandChild = GetAllRoots();

            Assert.That(initialState.Count, Is.EqualTo(0), "No events yet, expected there to be zero agg roots in the repo");

            Assert.That(afterBearingTwoChildren.Count, Is.EqualTo(3));

            var idsOfChildren = afterBearingTwoChildren
                .OfType<ProgrammerAggregate>()
                .Single(p => p.Id == firstAggregateRootId)
                .GetIdsOfChildren();

            Assert.That(idsOfChildren, Is.EqualTo(new[] { firstChildId, secondChildId }));

            Assert.That(afterBearingGrandChild.Count, Is.EqualTo(4));

            var idsOfGrandChildren = afterBearingGrandChild
                .OfType<ProgrammerAggregate>()
                .Single(p => p.Id == firstChildId)
                .GetIdsOfChildren();

            Assert.That(idsOfGrandChildren, Is.EqualTo(new[] { grandChildId }));
        }

        List<AggregateRoot> GetAllRoots()
        {
            return Enumerable.Select<DomainEvent, Guid>(_eventStore, e => DomainEventExtensions.GetAggregateRootId(e)).Distinct()
                .Select(aggregateRootId => _aggregateRootRepository.Get<ProgrammerAggregate>(aggregateRootId).AggregateRoot)
                .Cast<AggregateRoot>()
                .ToList();
        }

        public class BearSomeChildrenCommand : Command<ProgrammerAggregate>
        {
            public Guid[] IdsOfChildren { get; private set; }

            public BearSomeChildrenCommand(Guid aggregateRootId, params Guid[] idsOfChildren)
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
            readonly List<Guid> _idsOfChildren = new List<Guid>();

            public void BearChildren(IEnumerable<Guid> idsOfChildren)
            {
                foreach (var id in idsOfChildren)
                {
                    Load<ProgrammerAggregate>(id, createIfNotExists: true).GiveBirth();
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

            public IEnumerable<Guid> GetIdsOfChildren()
            {
                return _idsOfChildren;
            }
        }

        public class HadChildren : DomainEvent<ProgrammerAggregate>
        {
            public HadChildren(IEnumerable<Guid> childrenIds)
            {
                ChildrenIds = childrenIds.ToArray();
            }

            public Guid[] ChildrenIds { get; private set; }
        }

        public class WasBorn : DomainEvent<ProgrammerAggregate>
        {

        }
    }
}