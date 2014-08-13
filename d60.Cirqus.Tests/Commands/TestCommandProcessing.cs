using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.TestHelpers.Internals;
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

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore();
            var eventDispatcher = new ConsoleOutEventDispatcher();

            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore);

            _cirqus = new CommandProcessor(_eventStore, _aggregateRootRepository, eventDispatcher);
        }

        [Test]
        public void CanLetSpecificExceptionTypesThrough()
        {
            _cirqus.Options.AddDomainException<InvalidOperationException>();

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

            Assert.That(Enumerable.ToList<DomainEvent>(_eventStore).Count, Is.EqualTo(1));
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

        [Test]
        public void ThrowsNiceExceptionForCommandDerivedOffOfTheWrongCommandTypes()
        {
            Assert.Throws<ArgumentException>(() => _cirqus.ProcessCommand(new SomeCommand()));
        }

        class SomeCommand : Command
        {

        }
    }
}