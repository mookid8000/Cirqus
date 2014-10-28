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
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer();

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
            _eventStore.Save(Guid.NewGuid(), new[] {someEvent}.Select(e => _domainEventSerializer.DoSerialize(e)));

            someEvent.ListOfStuff.Add("WHOA?!!? WHERE DID YOU COME FROM??");

            var allEvents = _eventStore.Stream().OfType<SomeEvent>().ToList();
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