using System;
using System.Linq;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCustomMetadata : FixtureBase
    {
        CommandProcessor _cirqus;
        DefaultAggregateRootRepository _aggregateRootRepository;
        InMemoryEventStore _eventStore;
        readonly DomainEventSerializer _domainEventSerializer = new DomainEventSerializer();

        protected override void DoSetUp()
        {
            _eventStore = new InMemoryEventStore(_domainEventSerializer);

            _aggregateRootRepository = new DefaultAggregateRootRepository(_eventStore, _domainEventSerializer);

            var viewManager = new ConsoleOutEventDispatcher();

            _cirqus = RegisterForDisposal(new CommandProcessor(_eventStore, _aggregateRootRepository, viewManager,
                _domainEventSerializer));
        }

        [Test]
        public void CopiesCustomHeadersToEmittedEvents()
        {
            var programmer1Id = Guid.NewGuid();
            var programmer2Id = Guid.NewGuid();
            var programmer3Id = Guid.NewGuid();

            const string tenantId = "tenant-id";

            _cirqus.ProcessCommand(new TakeNextStepCommand(programmer1Id)
            {
                Meta = {{tenantId, "1"}}
            });
            
            _cirqus.ProcessCommand(new TakeNextStepCommand(programmer2Id)
            {
                Meta = {{tenantId, "2"}}
            });
            
            _cirqus.ProcessCommand(new TakeNextStepCommand(programmer3Id)
            {
                Meta = {{tenantId, "2"}}
            });

            var allEvents = _eventStore.ToList();

            Assert.That(allEvents.Count, Is.EqualTo(3));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 1), Is.EqualTo(1));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 2), Is.EqualTo(2));
        }

        public class TakeNextStepCommand : Command<ProgrammerAggregate>
        {
            public TakeNextStepCommand(Guid aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(ProgrammerAggregate aggregateRoot)
            {
                aggregateRoot.TakeNextStep();
            }
        }

        public class ProgrammerAggregate : AggregateRoot, 
            IEmit<FinishedEducation>,
            IEmit<LearnedAboutFunctionalProgramming>
        {
            enum ProgrammerState
            {
                Born,
                Educated,
                Knowing
            }

            ProgrammerState currentState;
            public string GetCurrentState()
            {
                return currentState.ToString();
            }

            public void TakeNextStep()
            {
                switch (currentState)
                {
                    case ProgrammerState.Born:
                        Emit(new FinishedEducation());
                        break;
                    case ProgrammerState.Educated:
                        Emit(new LearnedAboutFunctionalProgramming());
                        break;
                    case ProgrammerState.Knowing:
                        // just keep on knowing
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(string.Format("Unknown current state: {0}", currentState));
                }
            }

            public void Apply(FinishedEducation e)
            {
                currentState = ProgrammerState.Educated;
            }

            public void Apply(LearnedAboutFunctionalProgramming e)
            {
                currentState = ProgrammerState.Knowing;
            }
        }

        public class FinishedEducation : DomainEvent<ProgrammerAggregate>
        {

        }

        public class LearnedAboutFunctionalProgramming : DomainEvent<ProgrammerAggregate>
        {

        }
    }
}