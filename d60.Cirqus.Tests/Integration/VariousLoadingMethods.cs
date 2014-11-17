using System;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Exceptions;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Integration
{
    [TestFixture]
    public class VariousLoadingMethods : FixtureBase
    {
        readonly JsonDomainEventSerializer _serializer = new JsonDomainEventSerializer();
        ICommandProcessor _commandProcessor;
        Task<InMemoryEventStore> _eventStore;

        protected override void DoSetUp()
        {
            _commandProcessor = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .Options(o => o.UseCustomDomainEventSerializer(_serializer))
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public void CanCreateAggregateRoot()
        {
            var command = new GenericCommand(c =>
            {
                c.Create<SomeAggregateRoot>("doesNotExist");
                c.Create<SomeAggregateRoot>("doesNotExistEither");
            });

            _commandProcessor.ProcessCommand(command);

            var createdEvents = _eventStore.Result.Stream()
                .Select(e => _serializer.Deserialize(e))
                .OfType<SomeAggregateRootCreated>()
                .ToList();

            Assert.That(createdEvents.Count, Is.EqualTo(2));
        }

        [Test]
        public void CreateThrowsWhenAggregateRootInstanceAlreadyExists()
        {
            const string someAggregateRootId = "just/an/id";

            _commandProcessor.ProcessCommand(new GenericCommand(c => c.Create<SomeAggregateRoot>(someAggregateRootId)));

            var commandProcessingException = Assert.Throws<CommandProcessingException>(() =>
            {
                var commandThatShouldFail = new GenericCommand(c => c.Create<SomeAggregateRoot>(someAggregateRootId));

                _commandProcessor.ProcessCommand(commandThatShouldFail);
            });

            var invalidOperationException = (InvalidOperationException)commandProcessingException.InnerException;
            Assert.That(invalidOperationException.Message, Contains.Substring("already exists"));
        }

        [Test]
        public void TryLoadAggregateRootWhenItExists()
        {
            const string aggregateRootId = "doesNotExist";

            _commandProcessor.ProcessCommand(new GenericCommand(c => c.Create<SomeAggregateRoot>(aggregateRootId)));
            var gotAnInstance = false;

            _commandProcessor.ProcessCommand(new GenericCommand(c =>
            {
                var instance = c.TryLoad<SomeAggregateRoot>(aggregateRootId);

                gotAnInstance = instance != null;
            }));

            Assert.That(gotAnInstance, Is.True);
        }

        [Test]
        public void TryLoadAggregateRootWhenItDoesNotExist()
        {
            var gotAnInstance = false;
            var command = new GenericCommand(c =>
            {
                var instance = c.TryLoad<SomeAggregateRoot>("doesNotExist");

                gotAnInstance = instance != null;
            });

            _commandProcessor.ProcessCommand(command);

            Assert.That(gotAnInstance, Is.False);
        }

        [Test]
        public void LoadAggregateRootWhenItExists()
        {
            const string aggregateRootId = "another/aggregate/root/id";

            _commandProcessor.ProcessCommand(new GenericCommand(c => c.Create<SomeAggregateRoot>(aggregateRootId)));
            var gotAnInstance = false;

            _commandProcessor.ProcessCommand(new GenericCommand(c =>
            {
                var instance = c.Load<SomeAggregateRoot>(aggregateRootId);

                gotAnInstance = instance != null;
            }));

            Assert.That(gotAnInstance, Is.True);
        }

        [Test]
        public void LoadAggregateRootWhenItDoesNotExist()
        {
            var command = new GenericCommand(c => c.Load<SomeAggregateRoot>("doesNotExist"));

            var exception = Assert.Throws<CommandProcessingException>(() => _commandProcessor.ProcessCommand(command));
            var invalidOperationException = (ArgumentException)exception.InnerException;

            Assert.That(invalidOperationException.Message, Contains.Substring("it didn't exist"));
        }

        class GenericCommand : ExecutableCommand
        {
            readonly Action<ICommandContext> _action;

            public GenericCommand(Action<ICommandContext> action)
            {
                _action = action;
            }

            public override void Execute(ICommandContext context)
            {
                _action(context);
            }
        }

        class SomeAggregateRoot : AggregateRoot, IEmit<AnEvent>, IEmit<SomeAggregateRootCreated>
        {
            protected override void Created()
            {
                Emit(new SomeAggregateRootCreated());
            }

            public void Apply(AnEvent e)
            {

            }

            public void Apply(SomeAggregateRootCreated e)
            {

            }
        }

        class SomeAggregateRootCreated : DomainEvent<SomeAggregateRoot> { }

        class AnEvent : DomainEvent<SomeAggregateRoot> { }
    }
}