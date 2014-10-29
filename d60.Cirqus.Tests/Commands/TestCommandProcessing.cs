using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCommandProcessing : FixtureBase
    {
        CommandProcessor _cirqus;
        DefaultAggregateRootRepository _aggregateRootRepository;
        InMemoryEventStore _eventStore;
        readonly JsonDomainEventSerializer _domainEventSerializer = new JsonDomainEventSerializer();

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore(_domainEventSerializer);
            var eventDispatcher = new ConsoleOutEventDispatcher();

            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer);

            _cirqus = RegisterForDisposal(new CommandProcessor(_eventStore, _aggregateRootRepository, eventDispatcher,
                _domainEventSerializer));
        }

        [Test]
        public void CanProcessBaseCommand()
        {
            var aggregateRootIds = Enumerable.Range(0, 5).Select(i => Guid.NewGuid()).ToArray();
            var command = new MyCommand{AggregateRootIds = aggregateRootIds};

            _cirqus.ProcessCommand(command);

            var events = _eventStore.ToList();
            Assert.That(events.Count, Is.EqualTo(10));
        }

        public class MyRoot : AggregateRoot, IEmit<MyEvent>
        {
            public void EmitMyEvent()
            {
                Emit(new MyEvent());
            }

            public void Apply(MyEvent e)
            {
                
            }
        }

        public class MyEvent : DomainEvent<MyRoot> { }

        public class MyCommand : Command
        {
            public Guid[] AggregateRootIds { get; set; }

            public override void Execute(ICommandContext context)
            {
                AggregateRootIds.Select(context.Load<MyRoot>).ToList().ForEach(r => r.EmitMyEvent());
                AggregateRootIds.Select(context.Load<MyRoot>).ToList().ForEach(r => r.EmitMyEvent());
            }
        }


        [Test]
        public void CanLetSpecificExceptionTypesThrough()
        {
            _cirqus.Options.AddDomainExceptionType<InvalidOperationException>();

            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _cirqus.ProcessCommand(new ErronousCommand(Guid.NewGuid())));

            Console.WriteLine(unwrappedException);

            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void GeneratesPrettyException()
        {
            var appEx = Assert.Throws<CommandProcessingException>(() => _cirqus.ProcessCommand(new ErronousCommand(Guid.NewGuid())));

            Console.WriteLine(appEx);

            var inner = appEx.InnerException;

            Assert.That(inner, Is.TypeOf<InvalidOperationException>());
            Assert.That(inner.Message, Contains.Substring("oh no, you cannot do that"));
        }

        class ErronousCommand : Command<Root>
        {
            public ErronousCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                throw new InvalidOperationException("oh no, you cannot do that");
            }
        }

        [Test]
        public void CanProcessMappedCommand()
        {
            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new MappedCommand(aggregateRootId));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class MappedCommand : Command<Root>
        {
            public MappedCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Inc();
            }
        }

        [Test]
        public void CanProcessOrdinaryCommand()
        {
            var aggregateRootId = Guid.NewGuid();

            _cirqus.ProcessCommand(new OrdinaryCommand(aggregateRootId));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class OrdinaryCommand : Command<Root>
        {
            public OrdinaryCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Inc();
            }
        }

        [Test]
        public void ThrowsNiceExceptionForCommandThatHasNotBeenMapped()
        {
            Assert.Throws<CommandProcessingException>(() => _cirqus.ProcessCommand(new AnotherCommand(Guid.NewGuid())));
        }

        class AnotherCommand : Command<Root>
        {
            public AnotherCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                throw new NotImplementedException("huigehugiehwugiehw hugehugeiwgh hugiewhugie whguiohwugewgoewa huigehugiehwugiehw hugehugeiwgh hugiewhugie whguiohwugewgoewa");
            }
        }

        public class Root : AggregateRoot, IEmit<AnEvent>
        {
            public void Inc()
            {
                Emit(new AnEvent());
            }

            public void Apply(AnEvent e)
            {
            }
        }

        public class AnEvent : DomainEvent<Root>
        {
        }
    }
}