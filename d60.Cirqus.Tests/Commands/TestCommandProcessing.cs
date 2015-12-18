using System;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCommandProcessing : FixtureBase
    {
        ICommandProcessor _cirqus;
        Task<InMemoryEventStore> _eventStore;

        protected override void DoSetUp()
        {
            var commandMappings = new CommandMappings()
                .Map<CustomMappedErronousCommand>((context, command) =>
                {
                    throw new InvalidOperationException("oh no, you cannot do that");
                });

            _cirqus = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseEventDispatcher(c => 
                    new ConsoleOutEventDispatcher(c.Get<IEventStore>())))
                .Options(o =>
                {
                    o.AddDomainExceptionType<InvalidOperationException>();
                    o.AddCommandMappings(commandMappings);
                })
                .Create();

            RegisterForDisposal(_cirqus);
        }

        [Test]
        public void CanProcessBaseCommand()
        {
            var aggregateRootIds = Enumerable.Range(0, 5).Select(i => i.ToString()).ToArray();
            var command = new MyCommand { AggregateRootIds = aggregateRootIds };

            _cirqus.ProcessCommand(command);

            var events = _eventStore.Result.ToList();
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
                AggregateRootIds.Select(x => (context.TryLoad<MyRoot>(x) ?? context.Create<MyRoot>(x))).ToList().ForEach(r => r.EmitMyEvent());
                AggregateRootIds.Select(x => (context.TryLoad<MyRoot>(x) ?? context.Create<MyRoot>(x))).ToList().ForEach(r => r.EmitMyEvent());
            }
        }


        [Test]
        public void CanLetSpecificExceptionTypesThrough()
        {
            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _cirqus.ProcessCommand(new CommandThatThrowsDomainException("someid1")));

            Console.WriteLine(unwrappedException);

            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void CanLetSpecificExceptionTypesThroughAlsoWhenUsingCustomMappedCommands()
        {
            var unwrappedException = Assert.Throws<InvalidOperationException>(() => _cirqus.ProcessCommand(new CustomMappedErronousCommand()));

            Console.WriteLine(unwrappedException);

            Assert.That(unwrappedException, Is.TypeOf<InvalidOperationException>());
            Assert.That(unwrappedException.Message, Contains.Substring("oh no, you cannot do that"));
        }

        [Test]
        public void GeneratesPrettyException()
        {
            var appEx = Assert.Throws<CommandProcessingException>(() => _cirqus.ProcessCommand(new CommandThatThrowsUnanticipatedException("someid1")));

            Console.WriteLine(appEx);

            var inner = appEx.InnerException;

            Assert.That(inner, Is.TypeOf<ApplicationException>());
            Assert.That(inner.Message, Contains.Substring("oh no, you cannot do that"));
        }

        class CommandThatThrowsDomainException : Command<Root>
        {
            public CommandThatThrowsDomainException(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                throw new InvalidOperationException("oh no, you cannot do that");
            }
        }

        class CommandThatThrowsUnanticipatedException : Command<Root>
        {
            public CommandThatThrowsUnanticipatedException(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Root aggregateRoot)
            {
                throw new ApplicationException("oh no, you cannot do that");
            }
        }

        class CustomMappedErronousCommand : Command
        {
        }

        [Test]
        public void CanProcessMappedCommand()
        {
            _cirqus.ProcessCommand(new MappedCommand("id"));

            Assert.That(_eventStore.Result.ToList().Count, Is.EqualTo(1));
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

            Assert.That(_eventStore.Result.ToList().Count, Is.EqualTo(1));
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

        public class AnotherEvent : DomainEvent<AnotherRoot>
        {

        }
        public class SomeEvent : DomainEvent<AnotherRoot>
        {

        }
        public class AnotherRoot : AggregateRoot, IEmit<SomeEvent>, IEmit<AnotherEvent>
        {

            public void EmitBothEvents()
            {
                Emit(new SomeEvent());
                Emit(new AnotherEvent());
            }
            public void Apply(SomeEvent e)
            {

            }

            public void Apply(AnotherEvent e)
            {

            }
        }
        public class AnotherOrdinaryCommand : Command<AnotherRoot>
        {
            public AnotherOrdinaryCommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(AnotherRoot aggregateRoot)
            {
                aggregateRoot.EmitBothEvents();
            }
        }


        public class ExecutableCommandTest : ExecutableCommand
        {
            public string[] AggregateRootIds { get; set; }

            public override void Execute(ICommandContext context)
            {
                AggregateRootIds.Select(x => (context.TryLoad<AnotherRoot>(x) ?? context.Create<AnotherRoot>(x))).ToList().ForEach(r => r.EmitBothEvents());
                AggregateRootIds.Select(x => (context.TryLoad<AnotherRoot>(x) ?? context.Create<AnotherRoot>(x))).ToList().ForEach(r => r.EmitBothEvents());
            }
        }
    }
}