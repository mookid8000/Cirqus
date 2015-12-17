using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                .EventStore(e => e.Register<IEventStore>(c => new SlowWrapper(new InMemoryEventStore())))
                .EventDispatcher(e => e.UseViewManagerEventDispatcher().WithWaitHandle(waitHandle))
                .Options(o => o.AddProfiler(profilero))
                .Create();

            RegisterForDisposal(commandProcessor);

            commandProcessor.ProcessCommand(new DoStuffCommand("rootid") { OtherRootId = "otherrootid" });

            profilero.Space();

            commandProcessor.ProcessCommand(new DoStuffCommand("rootid") { OtherRootId = "otherrootid" });


            var firstLines = profilero.Calls
                .TakeWhile(c => !string.IsNullOrWhiteSpace(c))
                .ToList();

            var nextLines = profilero.Calls
                .Skip(firstLines.Count())
                .SkipWhile(string.IsNullOrWhiteSpace)
                .ToList();

            Console.WriteLine("Recorded calls with not history");
            Console.WriteLine(string.Join(Environment.NewLine, firstLines));
            Console.WriteLine();
            Console.WriteLine("Recorded calls with 1 event in history for each aggregate root");
            Console.WriteLine(string.Join(Environment.NewLine, nextLines));

            Assert.That(string.Join("; ", firstLines.Select(l => l.Substring(0, l.IndexOf('.')))),
                Is.EqualTo("RecordAggregateRootGet 00:00:00; RecordAggregateRootGet 00:00:00; RecordAggregateRootGet 00:00:00; RecordAggregateRootGet 00:00:00"),
                "Expected that four loads (2 x (tryload + create)) were performed, each taking way less than a second");

            Assert.That(string.Join("; ", nextLines.Select(l => l.Substring(0, l.IndexOf('.')))),
                Is.EqualTo("RecordAggregateRootGet 00:00:01; RecordAggregateRootGet 00:00:01"),
                "Expected that two loads taking sligtly > 1 s each would have been performed");
        }

        public class DoStuffCommand : Command<Root>
        {
            public DoStuffCommand(string aggregateRootId) 
                : base(aggregateRootId)
            {
            }

            public string OtherRootId { get; set; }

            public override void Execute(Root aggregateRoot)
            {
                aggregateRoot.DoStuff(OtherRootId);
            }
        }

        public class Root : AggregateRoot, IEmit<SomethingHappenedInRoot>
        {
            public void DoStuff(string otherRootId)
            {
                Emit(new SomethingHappenedInRoot { IdOfAnotherRoot = otherRootId });

                (TryLoad<AnotherRoot>(otherRootId) ?? Create<AnotherRoot>(otherRootId)).DoStuff();
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
            public string IdOfAnotherRoot { get; set; }
        }

        public class SomethingHappenedInAnotherRoot : DomainEvent<AnotherRoot>
        {
        }

        public class Profilero : IProfiler
        {
            readonly List<string> _calls = new List<string>();

            public List<string> Calls
            {
                get { return _calls; }
            }

            public void Space()
            {
                _calls.Add("");
            }

            public void RecordAggregateRootGet(TimeSpan elapsed, Type aggregateRootType, string aggregateRootId)
            {
                _calls.Add(string.Format("RecordAggregateRootGet {0}, {1}", elapsed, aggregateRootId));
            }

            public void RecordAggregateRootExists(TimeSpan elapsed, string aggregateRootId)
            {
            }

            public void RecordEventBatchSave(TimeSpan elapsed, Guid batchId)
            {
            }

            public void RecordGlobalSequenceNumberGetNext(TimeSpan elapsed)
            {
            }

            public void RecordEventDispatch(TimeSpan elapsed)
            {
                
            }
        }
    }

    public class SlowWrapper : IEventStore
    {
        readonly IEventStore _innerEventStore;

        public SlowWrapper(IEventStore innerEventStore)
        {
            _innerEventStore = innerEventStore;
        }

        public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
        {
            foreach (var e in _innerEventStore.Load(aggregateRootId, firstSeq))
            {
                Thread.Sleep(1000);

                yield return e;
            }
        }

        public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
        {
            return _innerEventStore.Stream(globalSequenceNumber);
        }

        public long GetNextGlobalSequenceNumber()
        {
            return _innerEventStore.GetNextGlobalSequenceNumber();
        }

        public void Save(Guid batchId, IEnumerable<EventData> events)
        {
            _innerEventStore.Save(batchId, events);
        }
    }
}