using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events
{
    [TestFixture]
    public class TestInMemoryEventStore : FixtureBase
    {
        InMemoryEventStore _eventStore;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore(_domainEventSerializer);
        }

        [Test]
        public void ReplayedEventsAreClones()
        {
            var someEvent = new SomeEvent
            {
                ListOfStuff = { "hej", "med", "dig" },
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, Guid.NewGuid().ToString()},
                    {DomainEvent.MetadataKeys.SequenceNumber, 0.ToString(Metadata.NumberCulture)},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, 0.ToString(Metadata.NumberCulture)},
                }
            };
            var eventData = new[] {someEvent}
                .Select(e => _domainEventSerializer.Serialize(e))
                .ToList();

            _eventStore.Save(Guid.NewGuid(), eventData);

            someEvent.ListOfStuff.Add("WHOA?!!? WHERE DID YOU COME FROM??");

            var allEvents = _eventStore.Stream()
                .Select(e => _domainEventSerializer.Deserialize(e))
                .OfType<SomeEvent>().ToList();

            Assert.That(allEvents.Count, Is.EqualTo(1));

            var relevantEvent = allEvents[0];

            Assert.That(relevantEvent.ListOfStuff.Count, Is.EqualTo(3), "Oh noes! It appears that the event was changed: {0}", string.Join(" ", relevantEvent.ListOfStuff));
        }

        class Root : AggregateRoot { }

        class SomeEvent : DomainEvent<Root>
        {
            public SomeEvent()
            {
                ListOfStuff = new List<string>();
            }
            public List<string> ListOfStuff { get; set; }
        }
    }
}