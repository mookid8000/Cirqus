using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Tests.Stubs;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Replicator
{
    [TestFixture, Category(TestCategories.MongoDb)]
    public class ErrorHandline : FixtureBase
    {
        readonly Dictionary<Guid, int> _seqNos = new Dictionary<Guid, int>();

        Cirqus.Events.EventReplicator _replicator;
        MongoDbEventStore _source;
        MongoDbEventStore _destination;
        ListLoggerFactory _listLoggerFactory;

        protected override void DoSetUp()
        {
            _listLoggerFactory = new ListLoggerFactory();
            CirqusLoggerFactory.Current = _listLoggerFactory;

            _seqNos.Clear();

            var database = MongoHelper.InitializeTestDatabase();

            _source = new MongoDbEventStore(database, "EventSrc");
            _destination = new MongoDbEventStore(database, "EventDst");

            _replicator = new Cirqus.Events.EventReplicator(new ThrowsAnErrorOnceInAWhile(_source, 0.5),
                new ThrowsAnErrorOnceInAWhile(_destination, 0.5));

            RegisterForDisposal(_replicator);

            _replicator.Start();
        }

        [TestCase(10)]
        [TestCase(20)]
        [TestCase(100)]
        [TestCase(200, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(1000, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(10000, Ignore = TestCategories.IgnoreLongRunning)]
        public void CanReplicateEventsEvenWhenErrorsOccurVeryOften(int numberOfEvents)
        {
            try
            {
                Enumerable.Range(0, numberOfEvents)
                    .Select(i => CreateNewEvent(Guid.NewGuid(), "event no " + i))
                    .ToList()
                    .ForEach(e => _source.Save(Guid.NewGuid(), new[] {e}));

                while (_destination.GetNextGlobalSequenceNumber() < numberOfEvents)
                {
                    Thread.Sleep(numberOfEvents);
                }

                Thread.Sleep(500);

                var myKindOfEvents = _destination
                    .Stream()
                    .ToList()
                    .OfType<Event>()
                    .ToList();

                Assert.That(myKindOfEvents.Count, Is.EqualTo(numberOfEvents));
                Assert.That(myKindOfEvents.Select(e => e.Data),
                    Is.EqualTo(Enumerable.Range(0, numberOfEvents).Select(i => "event no " + i)));

            }
            finally
            {
                Console.WriteLine("Got {0} errors while running the test...",
                    _listLoggerFactory.LoggedLines.Count(l => l.Level == Logger.Level.Error));
            }
        }

        Event CreateNewEvent(Guid aggregateRootId, string data)
        {
            return new Event
            {
                Data = data,
                Meta =
                {
                    {DomainEvent.MetadataKeys.AggregateRootId, aggregateRootId.ToString()},
                    {DomainEvent.MetadataKeys.SequenceNumber, GetNextSeqNoFor(aggregateRootId)},
                }
            };
        }

        int GetNextSeqNoFor(Guid aggregateRootId)
        {
            if (!_seqNos.ContainsKey(aggregateRootId))
                _seqNos[aggregateRootId] = 0;

            return _seqNos[aggregateRootId]++;
        }


        class Event : DomainEvent
        {
            public string Data { get; set; }
        }

        class ThrowsAnErrorOnceInAWhile : IEventStore
        {
            readonly IEventStore _innerEventStore;
            readonly double _errorProbability;
            readonly Random _random = new Random();

            public ThrowsAnErrorOnceInAWhile(IEventStore innerEventStore, double errorProbability)
            {
                _innerEventStore = innerEventStore;
                _errorProbability = errorProbability;
            }

            public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
            {
                PossiblyThrowError();

                _innerEventStore.Save(batchId, batch);
            }

            public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
            {
                PossiblyThrowError();

                return _innerEventStore.Load(aggregateRootId, firstSeq);
            }

            public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
            {
                PossiblyThrowError();

                return _innerEventStore.Stream(globalSequenceNumber);
            }

            public long GetNextGlobalSequenceNumber()
            {
                PossiblyThrowError();

                return _innerEventStore.GetNextGlobalSequenceNumber();
            }

            void PossiblyThrowError()
            {
                if (_random.NextDouble() > _errorProbability) return;

                throw new ApplicationException("OH NOES!! (artificial error, generated by the erronous event store decorator....)");
            }
        }
    }
}