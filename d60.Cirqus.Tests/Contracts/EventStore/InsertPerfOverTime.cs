using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Contracts.EventStore.Factories;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.EventStore
{
    [TestFixture(typeof(MongoDbEventStoreFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(InMemoryEventStoreFactory))]
    [TestFixture(typeof(MsSqlEventStoreFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(PostgreSqlEventStoreFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(NtfsEventStoreFactory))]
    public class InsertPerfOverTime<TEventStoreFactory> : FixtureBase where TEventStoreFactory : IEventStoreFactory, new()
    {
        readonly Dictionary<Guid, long> _seqNoPerRoot = new Dictionary<Guid, long>();
        readonly Guid[] _aggregateRootIds = Enumerable.Range(0, 100).Select(n => Guid.NewGuid()).ToArray();
        readonly Random _random = new Random(DateTime.Now.GetHashCode());
        readonly List<RecordedTime> _recordedTimes = new List<RecordedTime>();

        TEventStoreFactory _eventStoreFactory;
        IEventStore _eventStore;

        protected override void DoSetUp()
        {
            _recordedTimes.Clear();
            _seqNoPerRoot.Clear();
            _globalSeqNo = 0;

            _eventStoreFactory = new TEventStoreFactory();

            _eventStore = _eventStoreFactory.GetEventStore();

            if (_eventStore is IDisposable)
                RegisterForDisposal((IDisposable)_eventStore);
        }

        [TestCase(60, 20)]
        public void GenerateSaveReport(int testSeconds, int timeIntervals)
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Running test for {0} s", testSeconds);

            while (stopwatch.Elapsed < TimeSpan.FromSeconds(testSeconds))
            {
                var insertStopwatch = Stopwatch.StartNew();

                const int repetitions = 10;
                const int numberOfEventsPerRepetition = 10;

                repetitions.Times(() => Insert(numberOfEventsPerRepetition));

                RecordTimes(stopwatch, insertStopwatch, repetitions*numberOfEventsPerRepetition);
            }

            PresentResults(timeIntervals);
        }

        void PresentResults(int numberOfTimeIntervals)
        {
            var firstTime = _recordedTimes.OrderBy(t => t.When).First().When;
            var lastTime = _recordedTimes.OrderBy(t => t.When).Last().When;

            var totalDuration = lastTime - firstTime;
            if (totalDuration <= TimeSpan.Zero)
            {
                Console.WriteLine("No results");
                return;
            }

            var ticksPerInterval = totalDuration.Ticks / numberOfTimeIntervals;

            var intervalGroups = _recordedTimes
                .GroupBy(t => RoundToInterval(t.When, ticksPerInterval))
                .Select(g => new
                {
                    When = g.Key,
                    ElapsedSeconds = g.Sum(r => r.Elapsed.TotalSeconds),
                    NumberOfEvents = g.Sum(r => r.NumberOfEvents)
                })
                .OrderBy(g => g.When)
                .ToList();

            var max = intervalGroups.Max(g => g.ElapsedSeconds);

            const int width = 80;
            const int scaleWidth = 20;
            const int totalWidth = width + scaleWidth;

            Console.WriteLine(new string('-', totalWidth));
            Console.WriteLine(string.Join(Environment.NewLine, intervalGroups.Select(g => FormatGroup(g.When, g.ElapsedSeconds, max, width, scaleWidth, g.NumberOfEvents))));
            Console.WriteLine(new string('-', totalWidth));

            var min = 0;
            var minString = min.ToString("0");
            var maxString = ((int)max).ToString("0");

            Console.WriteLine("{0}{1}{2}{3}", new string(' ', scaleWidth), minString, new string(' ', width - minString.Length - maxString.Length), maxString);
            Console.WriteLine(new string('-', totalWidth));

            Console.WriteLine("Got {0} groups", intervalGroups.Count);
        }

        string FormatGroup(TimeSpan when, double elapsedSeconds, double max, int width, int scaleWidth, int numberOfEvents)
        {
            var valueWidth = width - scaleWidth;

            var charWidth = (int)(valueWidth * elapsedSeconds / max);

            var time = string.Format("{0:##0}:{1:00}.{2:0}", when.TotalMinutes, when.Seconds, when.Milliseconds/100)
                .PadRight(scaleWidth);

            var eventsPerSecondString = string.Format("{0:0} e/s", numberOfEvents / elapsedSeconds);

            return string.Format("{0}: {1}{2}{3:0}", time, new string('*', charWidth), new string(' ', valueWidth - charWidth + scaleWidth - eventsPerSecondString.Length), eventsPerSecondString);
        }

        TimeSpan RoundToInterval(TimeSpan timeSpan, long ticksPerInterval)
        {
            var foo = (int)timeSpan.Ticks / ticksPerInterval;
            var result = TimeSpan.FromTicks(foo * ticksPerInterval);

            return result;
        }

        class RecordedTime
        {
            public TimeSpan When { get; set; }
            public TimeSpan Elapsed { get; set; }
            public int NumberOfEvents { get; set; }
        }

        void RecordTimes(Stopwatch stopwatch, Stopwatch insertStopwatch, int numberOfEventsInserted)
        {
            _recordedTimes.Add(new RecordedTime
            {
                When = stopwatch.Elapsed, 
                Elapsed = insertStopwatch.Elapsed,
                NumberOfEvents = numberOfEventsInserted
            });
        }

        void Insert(int numberOfEvents)
        {
            _eventStore.Save(Guid.NewGuid(), Enumerable.Range(0, numberOfEvents)
                .Select(n => CreateEventWithRealisticPayload()));
        }

        long GetNextFor(Guid aggregateRootId)
        {
            if (!_seqNoPerRoot.ContainsKey(aggregateRootId))
                _seqNoPerRoot[aggregateRootId] = 0;

            return _seqNoPerRoot[aggregateRootId]++;
        }

        long _globalSeqNo;

        DomainEvent CreateEventWithRealisticPayload()
        {
            var aggregateRootId = _aggregateRootIds[_random.Next(_aggregateRootIds.Length)];
            var seqNo = GetNextFor(aggregateRootId);
            var globalSequenceNumber = _globalSeqNo++;

            return new EventWithRealisticPayload
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId},
                    {DomainEvent.MetadataKeys.SequenceNumber, seqNo},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, globalSequenceNumber},
                },
                AnInt = _random.Next(1000),
                SomeStringValue = string.Format("This is a random number: {0}", _random.Next(10)),
                Stuff = Enumerable.Range(0, _random.Next(4))
                    .Select(n => new Something
                    {
                        Text = string.Format("Thing # {0}", n)
                    })
                    .ToList()
            };
        }

        class EventWithRealisticPayload : DomainEvent
        {
            public string SomeStringValue { get; set; }
            public int AnInt { get; set; }
            public List<Something> Stuff { get; set; }
        }

        class Something
        {
            public string Text { get; set; }
        }
    }
}