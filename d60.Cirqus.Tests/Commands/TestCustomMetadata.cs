using System;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Tests.Extensions;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Commands
{
    [TestFixture]
    public class TestCustomMetadata : FixtureBase
    {
        ICommandProcessor _cirqus;
        Task<InMemoryEventStore> _eventStore;

        protected override void DoSetUp()
        {
            _cirqus = CommandProcessor.With()
                .EventStore(e => _eventStore = e.UseInMemoryEventStore())
                .EventDispatcher(e => e.UseConsoleOutEventDispatcher())
                .Create();

            RegisterForDisposal(_cirqus);
        }

        [Test]
        public void CopiesCustomHeadersToEmittedEvents()
        {
            const string tenantId = "tenant-id";

            _cirqus.ProcessCommand(new TakeNextStepCommand("programmer1")
            {
                Meta = {{tenantId, "1"}}
            });
            
            _cirqus.ProcessCommand(new TakeNextStepCommand("programmer2")
            {
                Meta = {{tenantId, "2"}}
            });
            
            _cirqus.ProcessCommand(new TakeNextStepCommand("programmer3")
            {
                Meta = {{tenantId, "2"}}
            });

            var allEvents = _eventStore.Result.ToList();

            Assert.That(allEvents.Count, Is.EqualTo(3));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 1), Is.EqualTo(1));
            Assert.That(allEvents.Count(e => int.Parse(e.Meta[tenantId].ToString()) == 2), Is.EqualTo(2));
        }

        public class TakeNextStepCommand : Command<ProgrammerAggregate>
        {
            public TakeNextStepCommand(string aggregateRootId)
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