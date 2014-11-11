using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.MongoDb;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Replicator
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class BasicReplication : FixtureBase
    {
        readonly Dictionary<Guid, int> _seqNos = new Dictionary<Guid, int>();
        readonly JsonDomainEventSerializer _serializer = new JsonDomainEventSerializer();

        EventReplicator _replicator;
        MongoDbEventStore _source;
        MongoDbEventStore _destination;

        protected override void DoSetUp()
        {
            _seqNos.Clear();

            var database = MongoHelper.InitializeTestDatabase();

            _source = new MongoDbEventStore(database, "EventSrc");
            _destination = new MongoDbEventStore(database, "EventDst");

            _replicator = new EventReplicator(_source, _destination);

            RegisterForDisposal(_replicator);

            _replicator.Start();
        }

        [Test]
        public void CanReplicateEventBatch()
        {
            _source.Save(Guid.NewGuid(), GetEventData("hej"));

            Thread.Sleep(1000);

            var allEventsInDestinationStore = GetAllEventsInDestinationStore();

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

            _source.Save(batchId1, GetEventData("hej"));
            _source.Save(batchId2, GetEventData("hej", "hej"));

            Thread.Sleep(1000);

            var myKindOfEvents = GetAllEventsInDestinationStore()
                .OfType<Event>()
                .ToList();

            Assert.That(myKindOfEvents.Count, Is.EqualTo(3), "Expected event 0, 1, 2 but got only {0}", string.Join(", ", myKindOfEvents.Select(e => e.GetGlobalSequenceNumber())));
            var expectedSourceBatchIds = new[]
            {
                batchId1.ToString(),
                batchId2.ToString(),
                batchId2.ToString()
            };

            Assert.That(myKindOfEvents.Select(e => e.Meta[EventReplicator.SourceEventBatchId]).ToArray(), Is.EqualTo(expectedSourceBatchIds));
        }

        List<DomainEvent> GetAllEventsInDestinationStore()
        {
            var attempt = 0;

            while (attempt++ < 3)
            {
                var result = _destination
                    .Stream()
                    .Select(e => _serializer.Deserialize(e))
                    .ToList();

                if (result.Any())
                {
                    return result;
                }

                Console.WriteLine("Didn't get any events in the {0}. attempt - sleeping one second", attempt);
                
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            return new List<DomainEvent>();
        }

        IEnumerable<EventData> GetEventData(params string[] data)
        {
            return data
                .Select(str => CreateNewEvent(Guid.NewGuid(), str))
                .Select(e => _serializer.Serialize(e))
                .ToArray();
        }


        Event CreateNewEvent(Guid aggregateRootId, string data)
        {
            return new Event
            {
                Data = data,
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString()},
                    {DomainEvent.MetadataKeys.SequenceNumber, GetNextSeqNoFor(aggregateRootId).ToString(Metadata.NumberCulture)},
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