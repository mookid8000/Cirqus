using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Tests.MongoDb;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Replicator
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class BasicReplication : FixtureBase
    {
        readonly Dictionary<Guid, int> _seqNos = new Dictionary<Guid, int>();

        Cirqus.Events.EventReplicator _replicator;
        MongoDbEventStore _source;
        MongoDbEventStore _destination;

        protected override void DoSetUp()
        {
            _seqNos.Clear();

            var database = MongoHelper.InitializeTestDatabase();

            _source = new MongoDbEventStore(database, "EventSrc");
            _destination = new MongoDbEventStore(database, "EventDst");

            _replicator = new Cirqus.Events.EventReplicator(_source, _destination);

            RegisterForDisposal(_replicator);

            _replicator.Start();
        }

        [Test]
        public void CanReplicateEventBatch()
        {
            _source.Save(Guid.NewGuid(), new[]
            {
                CreateNewEvent(Guid.NewGuid(), "hej")
            });

            Thread.Sleep(1000);

            var allEventsInDestinationStore = _destination.Stream().ToList();

            Assert.That(allEventsInDestinationStore.Count, Is.EqualTo(1));

            var myKindOfEvents = allEventsInDestinationStore.OfType<Event>().ToList();

            Assert.That(myKindOfEvents.Count, Is.EqualTo(1));
            Assert.That(myKindOfEvents[0].Data, Is.EqualTo("hej"));
        }

        [Test]
        public void ReplicatedEventsGetSourceBatchIdHeader()
        {
            var batchId1 = Guid.NewGuid();
            var batchId2 = Guid.NewGuid();

            _source.Save(batchId1, new[] { CreateNewEvent(Guid.NewGuid(), "hej") });
            _source.Save(batchId2, new[] { CreateNewEvent(Guid.NewGuid(), "hej"), CreateNewEvent(Guid.NewGuid(), "hej") });

            Thread.Sleep(1000);

            var myKindOfEvents = _destination.Stream().ToList().OfType<Event>().ToList();

            Assert.That(myKindOfEvents.Count, Is.EqualTo(3));
            var expectedSourceBatchIds = new[]
            {
                batchId1.ToString(),
                batchId2.ToString(),
                batchId2.ToString()
            };

            Assert.That(myKindOfEvents.Select(e => e.Meta[Cirqus.Events.EventReplicator.SourceEventBatchId]).ToArray(), Is.EqualTo(expectedSourceBatchIds));
        }


        Event CreateNewEvent(Guid aggregateRootId, string data)
        {
            return new Event
            {
                Data = data,
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString()},
                    {DomainEvent.MetadataKeys.SequenceNumber, GetNextSeqNoFor(aggregateRootId)},
                }
            };
        }


        int GetNextSeqNoFor(Guid aggregateRootId)
        {
            if (!_seqNos.ContainsKey(aggregateRootId))
                _seqNos[aggregateRootId] = 0;

            return _seqNos[aggregateRootId]++;
        }


        class Event : DomainEvent
        {
            public string Data { get; set; }
        }
    }
}