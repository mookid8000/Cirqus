using System;
using System.Collections.Generic;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Commands;
using d60.Cirqus.Config;
using d60.Cirqus.Diagnostics;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Testing.Internals;
using d60.Cirqus.Views.ViewManagers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Diagnostics
{
    [TestFixture]
    public class TestProfiler : FixtureBase
    {
        [Test]
        public void StatementOfSomething()
        {
            var waitHandle = new ViewManagerWaitHandle();
            var profilero = new Profilero();

            var commandProcessor = CommandProcessor.With()
                .Logging(l => l.UseConsole(minLevel:Logger.Level.Warn))
                .EventStore(e => e.Registrar.Register<IEventStore>(c => new InMemoryEventStore()))
                .EventDispatcher(e => e.UseViewManagerEventDispatcher(waitHandle))
                .Options(o => o.AddProfiler(profilero))
                .Create();

            RegisterForDisposal(commandProcessor);

            var rootId = new Guid("65F49D9D-C5C3-4E28-890A-E1D8B5763AD1");
            var otherRootId = new Guid("39E5BC53-B87E-4100-B436-F32E2FB576B0");

            commandProcessor.ProcessCommand(new DoStuffCommand(rootId) { OtherRootId = otherRootId });

            profilero.Space();

            commandProcessor.ProcessCommand(new DoStuffCommand(rootId) { OtherRootId = otherRootId });

            Console.WriteLine(string.Join(Environment.NewLine, profilero.Calls));
        }

        public class DoStuffCommand : Command<Root>
        {
            public DoStuffCommand(Guid aggregateRootId) : base(aggregateRootId)
            {
            }

            public Guid OtherRootId { get; set; }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff(OtherRootId);
            }
        }

        public class Root : AggregateRoot, IEmit<SomethingHappenedInRoot>
        {
            public void DoStuff(Guid otherRootId)
            {
                Emit(new SomethingHappenedInRoot { IdOfAnotherRoot = otherRootId });

                Load<AnotherRoot>(otherRootId, createIfNotExists:true).DoStuff();
            }

            public void Apply(SomethingHappenedInRoot e)
            {

            }
        }

        public class AnotherRoot : AggregateRoot, IEmit<SomethingHappenedInAnotherRoot>
        {
            public void DoStuff()
            {
                Emit(new SomethingHappenedInAnotherRoot());
            }

            public void Apply(SomethingHappenedInAnotherRoot e)
            {

            }
        }

        public class SomethingHappenedInRoot : DomainEvent<Root>
        {
            public Guid IdOfAnotherRoot { get; set; }
        }

        public class SomethingHappenedInAnotherRoot : DomainEvent<AnotherRoot>
        {
        }

        public class Profilero : IProfiler
        {
            readonly List<string> _calls = new List<string>();

            public void Space()
            {
                _calls.Add("");
            }

            public void RecordAggregateRootGet(TimeSpan elapsed, Type type, Guid aggregateRootId)
            {
                _calls.Add(string.Format("RecordAggregateRootGet {0}, {1}, {2}", elapsed, type, aggregateRootId));
            }

            public void RecordAggregateRootExists(TimeSpan elapsed, Guid aggregateRootId)
            {
                _calls.Add(string.Format("RecordAggregateRootExists {0}, {1}", elapsed, aggregateRootId));
            }

            public List<string> Calls
            {
                get { return _calls; }
            }

            public void RecordEventBatchSave(TimeSpan elapsed, Guid batchId)
            {
                _calls.Add(string.Format("RecordEventBatchSave {0}, {1}", elapsed, batchId));
            }

            public void RecordGlobalSequenceNumberGetNext(TimeSpan elapsed)
            {
                _calls.Add(string.Format("RecordGlobalSequenceNumberGetNext {0}", elapsed));
            }
        }
    }
}