using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCommandTypeNames : FixtureBase
    {
        ICommandProcessor _commandProcessor;
        Task<InMemoryEventStore> _eventStore;

        protected override void DoSetUp()
        {
            _commandProcessor = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseEventDispatcher(c => new ConsoleOutEventDispatcher()))
                .Options(o =>
                {
                    o.AddCommandTypeNameToMetadata();
                })
                .Create();

            RegisterForDisposal(_commandProcessor);
        }

        [Test]
        public async Task AddsCommandTypeNameToAllEmittedEventOnExecutableCommands()
        {
            var executableCommand = new Cwommand("bimse");

            //act
            _commandProcessor.ProcessCommand(executableCommand);

            //assert
            var events = (await _eventStore).ToList();

            const string expectedCommandTypeName = "d60.Cirqus.Tests.Commands.TestCommandTypeNames+Cwommand, d60.Cirqus.Tests";

            var commandTypeNamesPresent = events
                .Select(e =>
                {
                    if (!e.Meta.ContainsKey(DomainEvent.MetadataKeys.CommandTypeName))
                    {
                        throw new AssertionException(string.Format("Could not find the '{0}' key in the command's metadata - had only {1}",
                            DomainEvent.MetadataKeys.CommandTypeName, string.Join(", ", e.Meta.Select(kvp => string.Format("{0}={1}", kvp.Key, kvp.Value)))));
                    }

                    return e.Meta[DomainEvent.MetadataKeys.CommandTypeName];
                })
                .Distinct()
                .ToList();

            Assert.That(commandTypeNamesPresent.Count, Is.EqualTo(1));

            var actualCommandTypeName = commandTypeNamesPresent.Single();

            Assert.That(actualCommandTypeName, Is.EqualTo(expectedCommandTypeName));
        }

        class Woot : AggregateRoot, IEmit<Ewent>
        {
            public void DoStuff()
            {
                Emit(new Ewent());
            }

            public void Apply(Ewent e)
            {
            }
        }

        class Ewent : DomainEvent<Woot> { }

        class Cwommand : Command<Woot>
        {
            public Cwommand(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(Woot aggregateRoot)
            {
                aggregateRoot.DoStuff();
            }
        }
    }
}