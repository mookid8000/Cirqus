using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Aggregates
{
    [TestFixture]
    public class TestDefaultAggregateRootRepository : FixtureBase
    {
        InMemoryEventStore _eventStore;
        DefaultAggregateRootRepository _repository;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();
        readonly DefaultDomainTypeNameMapper _defaultDomainTypeNameMapper = new DefaultDomainTypeNameMapper();

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();
            _repository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer, _defaultDomainTypeNameMapper);
        }

        [TestCase(0, true, false)]
        [TestCase(1, true, false)]
        [TestCase(2, true, false)]
        [TestCase(3, true, true)]
        [TestCase(4, true, true)]
        public void CanDetermineWhetherRootExistsAtDifferentPointsInTime(long globalSequenceNumberToCheck, bool expectedExistenceOfRoot1, bool expectedExistenceOfRoot2)
        {
            SaveEvent("id1", 0, 0);
            SaveEvent("id1", 1, 1);
            SaveEvent("id1", 2, 2);
            SaveEvent("id2", 3, 0);
            SaveEvent("id2", 4, 1);

            Assert.That(_repository.Exists("id1", globalSequenceNumberToCheck), Is.EqualTo(expectedExistenceOfRoot1), 
                "Expected root1 existence to be {0} from global seq no {1}", expectedExistenceOfRoot1, globalSequenceNumberToCheck);
            Assert.That(_repository.Exists("id2", globalSequenceNumberToCheck), Is.EqualTo(expectedExistenceOfRoot2), 
                "Expected root2 existence to be {0} from global seq no {1}", expectedExistenceOfRoot2, globalSequenceNumberToCheck);
        }

        void SaveEvent(string aggregateRootId, long globalSeqNo, long seqNo)
        {
            _eventStore.Save(Guid.NewGuid(), new[]
            {
                new Event
                {
                    Meta =
                    {
                        {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                        {DomainEvent.MetadataKeys.GlobalSequenceNumber, globalSeqNo.ToString(Metadata.NumberCulture)},
                        {DomainEvent.MetadataKeys.SequenceNumber, seqNo.ToString(Metadata.NumberCulture)},
                    }
                }
            }
            .Select(e => _domainEventSerializer.Serialize(e)));
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