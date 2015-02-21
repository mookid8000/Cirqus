using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.NUnit;
using d60.Cirqus.Serialization;
using NUnit.Framework;
using TestContext = d60.Cirqus.Testing.TestContext;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestCirqusTestsOverrideTestContext : CirqusTests
    {
        FunnySerializer serializer;

        protected override TestContext CreateContext()
        {
            serializer = new FunnySerializer();
            return TestContext.With()
                .Options(x => x.UseCustomDomainEventSerializer(serializer))
                .Create();
        }

        [Test]
        public void CanOverrideTestContests()
        {
            var @event = new Event();
            Emit(NewId<Root>(), @event);
            Assert.AreEqual(@event, serializer.VipEvent);
        }

        public class Event : DomainEvent<Root> {}
        public class Root : AggregateRoot { }

        public class FunnySerializer : IDomainEventSerializer
        {
            public DomainEvent VipEvent { get; private set; }
            
            public EventData Serialize(DomainEvent e)
            {
                VipEvent = e;
                return EventData.FromDomainEvent(e, new byte[0]);
            }

            public DomainEvent Deserialize(EventData e)
            {
                return VipEvent;
            }
        }
    }
}