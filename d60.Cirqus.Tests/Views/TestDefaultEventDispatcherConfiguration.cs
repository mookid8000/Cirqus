using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views
{
    [TestFixture]
    public class TestDefaultEventDispatcherConfiguration
    {
        [Test]
        public void WorkWhenSpecifyingMinimalConfiguration()
        {
            using (var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Register<IEventStore>(c => new InMemoryEventStore(c.Get<IDomainEventSerializer>())))
                .Create())
            {
                commandProcessor.ProcessCommand(new Commando("id"));
            }
        }

        [Test]
        public void WorkWhenSpecifyingAggregateRootRepository()
        {
            using (var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.Register<IEventStore>(c => new InMemoryEventStore(c.Get<IDomainEventSerializer>())))
                .Create())
            {
                commandProcessor.ProcessCommand(new Commando("id"));
            }
        }

        public class Root : AggregateRoot, IEmit<Event>
        {
            public void Boom()
            {
                Emit(new Event());
            }

            public void Apply(Event e)
            {
            }
        }

        public class Event : DomainEvent<Root> { }

        public class Commando : Command<Root>
        {
            public Commando(string aggregateRootId) 
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Boom();
            }
        }
    }
}