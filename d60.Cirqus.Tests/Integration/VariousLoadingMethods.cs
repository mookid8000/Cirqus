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
using MongoDB.Driver.Linq;
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
            _commandProcessor.ProcessCommand(new GenericCommand(c => c.Create<SomeAggregateRoot>("doesNotExist")));

            var commandProcessingException = Assert.Throws<CommandProcessingException>(() =>
            {
                var commandThatShouldFail = new GenericCommand(c => c.Create<SomeAggregateRoot>("doesNotExist"));

                _commandProcessor.ProcessCommand(commandThatShouldFail);
            });

            var invalidOperationException = (InvalidOperationException)commandProcessingException.InnerException;
            Assert.That(invalidOperationException.Message, Contains.Substring("already exists"));
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