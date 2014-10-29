using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Tests.Diagnostics;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Serialization
{
    [TestFixture]
    public class TestJsonDomainEventSerializer : FixtureBase
    {
        JsonDomainEventSerializer _serializer;

        protected override void DoSetUp()
        {
            _serializer = new JsonDomainEventSerializer();
        }

        public class Root : AggregateRoot { }

        [Test]
        public void CanRoundtripMyEvent()
        {
            var anEvent = new MyEvent
            {
                ListOfStuff = new List<string> { "hej", "med", "dig" }
            };

            var eventData = _serializer.Serialize(anEvent);
            var roundtrippedEvent = (MyEvent)_serializer.Deserialize(eventData);

            Assert.That(roundtrippedEvent.ListOfStuff, Is.EqualTo(new[] { "hej", "med", "dig" }));
        }

        public class MyEvent : DomainEvent<Root>
        {
            public MyEvent()
            {
                Meta["bim"] = "hej!";
            }

            public List<string> ListOfStuff { get; set; }
        }

        [Test]
        public void CanRoundtripSimpleEvent()
        {
            var anEvent = new SimpleEvent
            {
                ListOfStuff = new List<string> { "hej", "med", "dig" }
            };

            var eventData = _serializer.Serialize(anEvent);
            var roundtrippedEvent = (SimpleEvent)_serializer.Deserialize(eventData);

            Assert.That(roundtrippedEvent.ListOfStuff, Is.EqualTo(new[] { "hej", "med", "dig" }));
        }

        public class SimpleEvent : DomainEvent<Root>
        {
            public List<string> ListOfStuff { get; set; }
        }

        [Test]
        public void CanRoundtripMostSimpleEvent()
        {
            var anEvent = new MostSimpleEvent { Text = "hej med dig" };

            var eventData = _serializer.Serialize(anEvent);
            var roundtrippedEvent = (MostSimpleEvent)_serializer.Deserialize(eventData);

            Assert.That(roundtrippedEvent.Text, Is.EqualTo("hej med dig"));
        }

        public class MostSimpleEvent : DomainEvent<Root>
        {
            public string Text { get; set; }
        }
    }
}