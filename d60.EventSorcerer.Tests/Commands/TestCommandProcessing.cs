using System;
using System.Linq;
using d60.EventSorcerer.Aggregates;
using d60.EventSorcerer.Commands;
using d60.EventSorcerer.Config;
using d60.EventSorcerer.Events;
using d60.EventSorcerer.Exceptions;
using d60.EventSorcerer.TestHelpers;
using d60.EventSorcerer.TestHelpers.Internals;
using NUnit.Framework;

namespace d60.EventSorcerer.Tests.Commands
{
    [TestFixture]
    public class TestCommandProcessing : FixtureBase
    {
        EventSorcererConfig _eventSorcerer;
        CommandMapper _commandMapper;
        DefaultAggregateRootRepository _aggregateRootRepository;
        InMemoryEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();
            var eventDispatcher = new ConsoleOutEventDispatcher();

            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);
            _commandMapper = new CommandMapper();

            _eventSorcerer = new EventSorcererConfig(_eventStore, _aggregateRootRepository, _commandMapper, eventDispatcher);
        }

        [Test]
        public void CanLetSpecificExceptionTypesThrough()
        {
            _eventSorcerer.Options.AddDomainException<InvalidOperationException>();
            
            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _eventSorcerer.ProcessCommand(new ErronousCommand(Guid.NewGuid())));

            Console.WriteLine(unwrappedException);
   
            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void GeneratesPrettyException()
        {
            var appEx = Assert.Throws<CommandProcessingException>(() => _eventSorcerer.ProcessCommand(new ErronousCommand(Guid.NewGuid())));

            Console.WriteLine(appEx);

            var inner = appEx.InnerException;
            
            Assert.That(inner, Is.TypeOf<InvalidOperationException>());
            Assert.That(inner.Message, Contains.Substring("oh no, you cannot do that"));
        }

        class ErronousCommand : MappedCommand<Root>
        {
            public ErronousCommand(Guid aggregateRootId) : base(aggregateRootId)
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

            _eventSorcerer.ProcessCommand(new MappedCommand(aggregateRootId));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class MappedCommand : MappedCommand<Root>
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
            _commandMapper.Map<OrdinaryCommand, Root>((c, r) => r.Inc());
            var aggregateRootId = Guid.NewGuid();

            _eventSorcerer.ProcessCommand(new OrdinaryCommand(aggregateRootId));

            Assert.That(_eventStore.ToList().Count, Is.EqualTo(1));
        }

        class OrdinaryCommand : Command<Root>
        {
            public OrdinaryCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
            }
        }

        [Test]
        public void ThrowsNiceExceptionForCommandThatHasNotBeenMapped()
        {
            Assert.Throws<CommandProcessingException>(() => _eventSorcerer.ProcessCommand(new AnotherCommand(Guid.NewGuid())));
        }

        class AnotherCommand : Command<Root> {
            public AnotherCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
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

        [Test]
        public void ThrowsNiceExceptionForCommandDerivedOffOfTheWrongCommandTypes()
        {
            Assert.Throws<ArgumentException>(() => _eventSorcerer.ProcessCommand(new SomeCommand()));
        }

        class SomeCommand : Command
        {
            
        }
    }
}