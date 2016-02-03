using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.MsSql.Events;
using d60.Cirqus.Numbers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.MsSql
{
    [TestFixture]
    public class TestMsSqlEventStorePerformance : FixtureBase
    {
        MsSqlEventStore _eventStore;

        readonly Random _random = new Random(DateTime.Now.GetHashCode());
        int _globalSequenceNumber;

        protected override void DoSetUp()
        {
            MsSqlTestHelper.DropTable("Events");

            _eventStore = new MsSqlEventStore(MsSqlTestHelper.ConnectionString, "Events");

            _globalSequenceNumber = 0;
        }

        [TestCase(10000)]
        public void CheckReadPerformance(int numberOfEvents)
        {
            var stopwatch = Stopwatch.StartNew();
            WriteEvents(numberOfEvents);
            var seconds = stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine("Writing {0} events tooke {1:0.0} s - that's {2:0.0} events/s", numberOfEvents, seconds, numberOfEvents / seconds);
        }

        void WriteEvents(int numberOfEvents)
        {
            var sequenceNumbers = new Dictionary<string, int>();

            foreach (var batch in Enumerable.Range(0, numberOfEvents).Batch(1000))
            {
                _eventStore.Save(Guid.NewGuid(),
                    batch.Select(i => EventData.FromMetadata(GetMeta(sequenceNumbers), FakeData(1024))));
            }
        }

        Metadata GetMeta(Dictionary<string, int> sequenceNumbers)
        {
            var meta = new Metadata();

            var aggregateRootId = GetRandomAggregateRootId();

            meta[DomainEvent.MetadataKeys.AggregateRootId] = aggregateRootId;
            meta[DomainEvent.MetadataKeys.SequenceNumber] = GetNext(sequenceNumbers, aggregateRootId);
            meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (_globalSequenceNumber++).ToString();

            return meta;
        }

        string GetRandomAggregateRootId()
        {
            return string.Format("agg-{0}", _random.Next(100));
        }

        string GetNext(Dictionary<string, int> sequenceNumbers, string aggregateRootId)
        {
            if (!sequenceNumbers.ContainsKey(aggregateRootId))
            {
                sequenceNumbers[aggregateRootId] = 0;
            }

            return (sequenceNumbers[aggregateRootId]++).ToString();
        }

        byte[] FakeData(int byteCount)
        {
            var buffer = new byte[byteCount];
            _random.NextBytes(buffer);
            return buffer;
        }
    }
}