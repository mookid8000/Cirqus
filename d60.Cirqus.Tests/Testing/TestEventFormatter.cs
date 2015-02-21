using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Testing;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestEventFormatter
    {
        EventFormatter eventFormatter;
        TestWriter writer;

        [SetUp]
        public void Setup()
        {
            writer = new TestWriter();
            eventFormatter =
                new EventFormatter(
                    new TextFormatter(
                        writer));
        }

        [Test]
        public void RenderDefault()
        {
            var @event = new SomeEventWithNoProps();
            eventFormatter.Format(null, @event);
            Assert.AreEqual("SomeEventWithNoProps", writer.Buffer);
        }

        [Test]
        [Ignore]
        public void RenderDefaultsWithMetadata()
        {
            var @event = new SomeEventWithNoProps();
            @event.Meta[DomainEvent.MetadataKeys.AggregateRootId] = "theid";
            @event.Meta[DomainEvent.MetadataKeys.SequenceNumber] = "1";
            @event.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = "10";
            @event.Meta[DomainEvent.MetadataKeys.Type] = "I'm the silent type";

            eventFormatter.Format(null, @event);

            Assert.AreEqual(
                "SomeEventWithNoProps : root_id(theid), seq(1), gl_seq(10), type(I'm the silent type)",
                writer.Buffer);
        }

        [Test]
        [Ignore]
        public void RenderFixedTemplate()
        {
            var @event = new SomeEventWithNoProps();
            
            eventFormatter.Format("Template", @event);
            
            Assert.AreEqual(
                "Template [ root_id: ? / seq: ? / gl_seq: ? ]",
                writer.Buffer);
        }

        public class SomeRoot : AggregateRoot
        {
            
        }

        public class SomeEventWithNoProps : DomainEvent<SomeRoot>
        {

        }

        public class SomeEvent : DomainEvent<SomeRoot>
        {
            public string NoHayBanda { get; set; }
            public int OleOgLone { get; set; }
        }
    }
}