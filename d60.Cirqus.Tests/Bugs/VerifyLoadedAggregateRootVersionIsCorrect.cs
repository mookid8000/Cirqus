using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Tests.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Bugs
{
    [TestFixture]
    public class VerifyLoadedAggregateRootVersionIsCorrect : FixtureBase
    {
        [Test]
        public async Task ItShallNotBeSo()
        {
            var waitHandle = new ViewManagerWaitHandle();
            var viewManager = new InMemoryViewManager<Wview>();

            var commandProcessor = CommandProcessor.With()
                .EventStore(e => e.UseInMemoryEventStore())
                .EventDispatcher(e =>
                {
                    e.UseViewManagerEventDispatcher(viewManager)
                        .WithWaitHandle(waitHandle);
                })
                .Options(o => o.SetMaxRetries(0))
                .Create();

            const string oneWootId = "oneWoot";
            const string anotherWootId = "anotherWoot";
            
            using (commandProcessor)
            {
                commandProcessor.ProcessCommand(new MakeAnotherWootDoItsThing(anotherWootId));

                commandProcessor.ProcessCommand(new MakeOneWootReadAnotherWoot(oneWootId, anotherWootId));

                commandProcessor.ProcessCommand(new MakeAnotherWootDoItsThing(anotherWootId));

                var lastResult = commandProcessor.ProcessCommand(new MakeOneWootReadAnotherWoot(oneWootId, anotherWootId));

                await waitHandle.WaitFor<Wview>(lastResult, TimeSpan.FromSeconds(10));

                var instance = viewManager.Load(GlobalInstanceLocator.GetViewInstanceId());

                instance.ToString();
            }
        }

        class Wview : IViewInstance<GlobalInstanceLocator>, ISubscribeTo<OneWootDidItsThing>
        {
            public Wview()
            {
                ReadLists = new List<List<int>>();
            }

            public string Id { get; set; }
            public long LastGlobalSequenceNumber { get; set; }
            public List<List<int>> ReadLists { get; private set; }

            public void Handle(IViewContext context, OneWootDidItsThing domainEvent)
            {
                var oneWoot = context.Load<OneWoot>(domainEvent.GetAggregateRootId());

                ReadLists.Add(oneWoot.ReadCounts.ToList());
            }
        }

        class MakeOneWootReadAnotherWoot : Command<OneWoot>
        {
            public string AnotherWootId { get; private set; }

            public MakeOneWootReadAnotherWoot(string aggregateRootId, string anotherWootId) : base(aggregateRootId)
            {
                AnotherWootId = anotherWootId;
            }

            public override void Execute(OneWoot aggregateRoot)
            {
                aggregateRoot.DoYourThing(AnotherWootId);
            }
        }

        class MakeAnotherWootDoItsThing : Command<AnotherWoot>
        {
            public MakeAnotherWootDoItsThing(string aggregateRootId)
                : base(aggregateRootId)
            {
            }

            public override void Execute(AnotherWoot aggregateRoot)
            {
                aggregateRoot.DoYourThing();
            }
        }

        class OneWoot : AggregateRoot, IEmit<OneWootDidItsThing>
        {
            readonly List<int> _readCounts = new List<int>();

            public void DoYourThing(string anotherWootId)
            {
                Emit(new OneWootDidItsThing(anotherWootId));

                Load<AnotherWoot>(anotherWootId).DoYourThing();

                Emit(new OneWootDidItsThing(anotherWootId));
            }

            public void Apply(OneWootDidItsThing e)
            {
                var anotherWoot = Load<AnotherWoot>(e.AnotherWootId);

                _readCounts.Add(anotherWoot.ThisIsHowManyTimeMyThingWasDone);
            }

            public List<int> ReadCounts
            {
                get { return _readCounts; }
            }
        }

        class OneWootDidItsThing : DomainEvent<OneWoot>
        {
            public string AnotherWootId { get; private set; }

            public OneWootDidItsThing(string anotherWootId)
            {
                AnotherWootId = anotherWootId;
            }
        }

        class AnotherWoot : AggregateRoot, IEmit<AnotherWootDidItsThing>
        {
            public int ThisIsHowManyTimeMyThingWasDone { get; set; }

            public void DoYourThing()
            {
                Emit(new AnotherWootDidItsThing());
            }

            public void Apply(AnotherWootDidItsThing e)
            {
                ThisIsHowManyTimeMyThingWasDone++;
            }
        }

        class AnotherWootDidItsThing : DomainEvent<AnotherWoot>
        {
        }
    }
}