using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.NUnit;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Testing
{
    [TestFixture]
    public class TestCirqusTests : CirqusTests
    {
        static TestWriter writer;

        static TestCirqusTests()
        {
            Writer = () =>
            {
                writer = new TestWriter();
                return writer;
            };
        }

        [Test]
        public void WriteGiven()
        {
            Emit(NewId<RootA>(), new EventA1());

            Assert.AreEqual(string.Format(
@"Given that:
  EventA1
    Id: {0}

", Id<RootA>()), writer.Buffer);
        }

        [Test]
        public void GivenWhenThen()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandA { Id = Id<RootA>() });

            Then(Id<RootA>(), new EventA2());

            Assert.AreEqual(string.Format(
@"Given that:
  EventA1
    Id: {0}

When users:
  CommandA
    Id: {0}

Then:
  √ EventA2

", Id<RootA>()), writer.Buffer);
        }

        [Test]
        public void GivenWhenThenWrongId()
        {
            Emit(NewId<RootA>(), new EventA1());

            When(new CommandA { Id = Id<RootA>() });

            Assert.Throws<AssertionException>(() => Then(NewId<RootA>(), new EventA2()));
        }

        public class RootA : AggregateRoot, IEmit<EventA1>, IEmit<EventA2>
        {
            public void DoA1()
            {
                Emit(new EventA1());
            }

            public void DoA2()
            {
                Emit(new EventA2());
            }

            public void Apply(EventA1 e)
            {
                
            }

            public void Apply(EventA2 e)
            {
                
            }
        }

        public class EventA1 : DomainEvent<RootA>
        {

        }

        public class EventA2 : DomainEvent<RootA>
        {

        }

        public class CommandA : ExecutableCommand
        {
            public string Id { get; set; }

            public override void Execute(ICommandContext context)
            {
                var rootA = context.Load<RootA>(Id);
                rootA.DoA2();
            }
        }
    }
}