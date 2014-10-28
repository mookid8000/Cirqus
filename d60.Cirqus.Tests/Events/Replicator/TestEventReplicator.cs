using System;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Replicator
{
    [TestFixture]
    public class TestEventReplicator : FixtureBase
    {
        [Test]
        public void DoesNotThrowWhenDisposingUnstartedReplicator()
        {
            // arrange
            var serializer = new DomainEventSerializer();
            var eventReplicator = new EventReplicator(new InMemoryEventStore(serializer), new InMemoryEventStore(serializer));

            // act
            eventReplicator.Dispose();

            // assert
        }

        [Test]
        public void TryReplicating()
        {
            var serializer = new DomainEventSerializer();
            var source = new InMemoryEventStore(serializer);
            var destination = new InMemoryEventStore(serializer);
            var seqNo = 0;

            Func<string, Event> getRecognizableEvent = text => serializer.Serialize(new RecognizableEvent(text)
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, "268DD0C0-529F-4242-9D53-601A88BB1813"},
                    {DomainEvent.MetadataKeys.SequenceNumber, (seqNo).ToString(Metadata.NumberCulture)},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, (seqNo++).ToString(Metadata.NumberCulture)},
                }
            });

            // arrange
            using (var eventReplicator = new EventReplicator(source, destination))
            {
                eventReplicator.Start();
                Thread.Sleep(TimeSpan.FromSeconds(2));

                // act
                source.Save(Guid.NewGuid(), new[] { getRecognizableEvent("hello") });
                source.Save(Guid.NewGuid(), new[] { getRecognizableEvent("there") });
                source.Save(Guid.NewGuid(), new[] { getRecognizableEvent("my") });
                source.Save(Guid.NewGuid(), new[] { getRecognizableEvent("friend") });

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // assert
            var greeting = string.Join(" ", destination
                .OfType<RecognizableEvent>()
                .Select(e => e.Id));

            Assert.That(greeting, Is.EqualTo("hello there my friend"));
        }


        public class RecognizableEvent : DomainEvent
        {
            public RecognizableEvent(string id)
            {
                Id = id;
            }

            public string Id { get; set; }
        }
    }
}