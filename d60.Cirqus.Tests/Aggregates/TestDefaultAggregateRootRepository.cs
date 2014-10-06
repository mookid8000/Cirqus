using System;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestDefaultAggregateRootRepository : FixtureBase
    {
        InMemoryEventStore _eventStore;
        DefaultAggregateRootRepository _repository;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();
            _repository = new DefaultAggregateRootRepository(_eventStore);
        }

        [TestCase(0, true, false)]
        [TestCase(1, true, false)]
        [TestCase(2, true, false)]
        [TestCase(3, true, true)]
        [TestCase(4, true, true)]
        public void CanDetermineWhetherRootExistsAtDifferentPointsInTime(long globalSequenceNumberToCheck, bool expectedExistenceOfRoot1, bool expectedExistenceOfRoot2)
        {
            var root1 = Guid.NewGuid();
            var root2 = Guid.NewGuid();
            
            SaveEvent(root1, 0, 0);
            SaveEvent(root1, 1, 1);
            SaveEvent(root1, 2, 2);
            SaveEvent(root2, 3, 0);
            SaveEvent(root2, 4, 1);

            Assert.That(_repository.Exists<Root>(root1, globalSequenceNumberToCheck), Is.EqualTo(expectedExistenceOfRoot1), "Expected root1 existence to be {0} from global seq no {1}", expectedExistenceOfRoot1, globalSequenceNumberToCheck);
            Assert.That(_repository.Exists<Root>(root2, globalSequenceNumberToCheck), Is.EqualTo(expectedExistenceOfRoot2), "Expected root2 existence to be {0} from global seq no {1}", expectedExistenceOfRoot2, globalSequenceNumberToCheck);
        }

        void SaveEvent(Guid aggregateRootId, long globalSeqNo, long seqNo)
        {
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                new Event
                {
                    Meta =
                    {
                        {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                        {DomainEvent.MetadataKeys.GlobalSequenceNumber, globalSeqNo},
                        {DomainEvent.MetadataKeys.SequenceNumber, seqNo},
                    }
                }
            });
        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            public void Apply(Event e)
            {

            }
        }

        public class Event : DomainEvent<AggregateRoot>
        {

        }
    }
}