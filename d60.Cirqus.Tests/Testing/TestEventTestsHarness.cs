using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.NUnit;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestTrinityFormatter
    {
        readonly EventFormatter eventFormatter;

        public TestTrinityFormatter()
        {
            eventFormatter = new EventFormatter();
        }

        [Test]
        public void RenderDefault()
        {
            var @event = new SomeEventWithNoProps();
            Assert.AreEqual(
                "SomeEvent",
                eventFormatter.Render(@event, null));
        }

        [Test]
        public void RenderDefaultsWithMetadata()
        {
            var @event = new SomeEventWithNoProps();
            @event.Meta[DomainEvent.MetadataKeys.AggregateRootId] = "theid";
            @event.Meta[DomainEvent.MetadataKeys.SequenceNumber] = "1";
            @event.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = "10";
            @event.Meta[DomainEvent.MetadataKeys.Type] = "I'm the silent type";

            Assert.AreEqual(
                "SomeEvent : root_id(theid), seq(1), gl_seq(10), type(I'm the silent type)",
                eventFormatter.Render(@event, null));
        }

        [Test]
        public void RenderFixedTemplate()
        {
            var @event = new SomeEventWithNoProps();
            Assert.AreEqual(
                "Template [ root_id: ? / seq: ? / gl_seq: ? ]",
                eventFormatter.Render(@event, "Template"));
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

    [TestFixture]
    public class TestCirqusTestsHarness : MyCirqusTests
    {
        [Test]
        public void DoSomething()
        {
            Emit("theid", new SomeEvent());
        }
    }

    public class SomeEvent : DomainEvent<SomeRoot>
    {
        
    }

    public class SomeRoot : AggregateRoot {}

    public class MyCirqusTests : CirqusTests
    {
        [Test]
        public void Test()
        {
            Emit(NewId<RootA>(), new EventA());

        }

        public class RootA : AggregateRoot
        {
            
        }

        public class EventA : DomainEvent<RootA>
        {
            
        }
    }

}