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

            var commandMapper = new CommandMappings()
                .Map<CustomMappedErronousCommand>((context, command) =>
                {
                    throw new InvalidOperationException("oh no, you cannot do that");
                })
                .CreateCommandMapperDecorator(new DefaultCommandMapper());

            _cirqus = RegisterForDisposal(new CommandProcessor(_eventStore, _aggregateRootRepository, eventDispatcher,
                _domainEventSerializer, commandMapper));
        }

        [Test]
        public void CanProcessBaseCommand()
        {
            var aggregateRootIds = Enumerable.Range(0, 5).Select(i => i.ToString()).ToArray();
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

        public class MyCommand : ExecutableCommand
        {
            public string[] AggregateRootIds { get; set; }

            public override void Execute(ICommandContext context)
            {
                AggregateRootIds.Select(x => context.Load<MyRoot>(x, createIfNotExists: true)).ToList().ForEach(r => r.EmitMyEvent());
                AggregateRootIds.Select(x => context.Load<MyRoot>(x, createIfNotExists: true)).ToList().ForEach(r => r.EmitMyEvent());
            }
        }


        [Test]
        public void CanLetSpecificExceptionTypesThrough()
        {
            _cirqus.Options.AddDomainExceptionType<InvalidOperationException>();

            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _cirqus.ProcessCommand(new ErronousCommand("someid1")));

            Console.WriteLine(unwrappedException);

            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void CanLetSpecificExceptionTypesThroughAlsoWhenUsingCustomMappedCommands()
        {
            _cirqus.Options.AddDomainExceptionType<InvalidOperationException>();

            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _cirqus.ProcessCommand(new CustomMappedErronousCommand()));

            Console.WriteLine(unwrappedException);

            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void GeneratesPrettyException()
        {
            var appEx = Assert.Throws<CommandProcessingException>(() => _cirqus.ProcessCommand(new ErronousCommand("someid1")));

            Console.WriteLine(appEx);

            var inner = appEx.InnerException;

            Assert.That(inner, Is.TypeOf<InvalidOperationException>());
            Assert.That(inner.Message, Contains.Substring("oh no, you cannot do that"));
        }

        class ErronousCommand : Command<Root>
        {
            public ErronousCommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                throw new InvalidOperationException("oh no, you cannot do that");
            }
        }

        class CustomMappedErronousCommand : Command
        {
        }

        [Test]
        public void CanProcessMappedCommand()
        {
            _cirqus.ProcessCommand(new MappedCommand("id"));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class MappedCommand : Command<Root>
        {
            public MappedCommand(string aggregateRootId) : base(aggregateRootId) { }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Inc();
            }
        }

        [Test]
        public void CanProcessOrdinaryCommand()
        {
            _cirqus.ProcessCommand(new OrdinaryCommand("id"));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class OrdinaryCommand : Command<Root>
        {
            public OrdinaryCommand(string aggregateRootId) : base(aggregateRootId) { }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.Inc();
            }
        }

        [Test]
        public void ThrowsNiceExceptionForCommandThatHasNotBeenMapped()
        {
            Assert.Throws<CommandProcessingException>(() => 
                _cirqus.ProcessCommand(new AnotherCommand("rootid")));
        }

        class AnotherCommand : Command<Root>
        {
            public AnotherCommand(string aggregateRootId) : base(aggregateRootId) { }

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