using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Numbers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;
using Timer = System.Timers.Timer;

namespace d60.Cirqus.Tests.MongoDb
{
    [TestFixture]
    public class TestMongoDbEventStore : FixtureBase
    {
        MongoDbEventStore _eventStore;
        MongoDatabase _mongoDatabase;

        protected override void DoSetUp()
        {
            _mongoDatabase = MongoHelper.InitializeTestDatabase();
            _eventStore = new MongoDbEventStore(_mongoDatabase, "Events");
        }

        //[TestCase(1000)]
        //[TestCase(10000)]
        [TestCase(100000)]
        //[TestCase(1000000, Ignore = TestCategories.IgnoreLongRunning)]
        //[TestCase(10000000, Ignore = TestCategories.IgnoreLongRunning)]
        public void VerifyLazinessOfStreamingApi(int eventCount)
        {
            Console.Write("Generating {0} events... ", eventCount);
            QuicklyGenerateLoadsOfEvents(eventCount);
            Console.WriteLine(" Done!");

            var counter = 0L;

            Action print = () =>
            {
                Console.WriteLine("Iterated through {0} events... (ram: {1:0.0})", Interlocked.Read(ref counter), Process.GetCurrentProcess().WorkingSet64 / 1000000);
            };

            TakeTime(string.Format("load {0} events", eventCount), () =>
            {
                using (var timer = new Timer(5000))
                {
                    timer.Elapsed += delegate { print(); };
                    timer.Start();

                    foreach (var e in _eventStore.Stream())
                    {
                        Interlocked.Increment(ref counter);
                    }
                }
            });

            print();
        }

        /// <summary>
        /// Load w. query:
        /// 
        /// Using Mongo database 'cirqus_test'
        /// Dropping Mongo database 'cirqus_test'
        /// Generating 100000 events distributed among 1000 roots...
        /// Begin: load 100000/1000 = 100 events
        /// End: load 100000/1000 = 100 events - elapsed: 2,3 s
        /// Iterated through 100000 events... (ram: 167,0)
        /// 
        /// Load w. aggregation framework:
        /// 
        /// Using Mongo database 'cirqus_test'
        /// Dropping Mongo database 'cirqus_test'
        /// Generating 100000 events distributed among 1000 roots...
        /// Begin: load 100000/1000 = 100 events
        /// Iterated through 700 events... (ram: 168,0)
        /// Iterated through 1500 events... (ram: 168,0)
        /// Iterated through 2200 events... (ram: 169,0)
        /// (.......)
        /// Iterated through 99000 events... (ram: 44,0)
        /// Iterated through 99700 events... (ram: 44,0)
        /// End: load 100000/1000 = 100 events - elapsed: 662,0 s
        /// Iterated through 100000 events... (ram: 44,0)
        /// 
        /// Conclusion: Use queries :)
        /// </summary>
        [TestCase(100000, 1000)]
        public void TestPerformanceOfAggregateRootEventStreaming(int eventCount, int rootCount)
        {
            var rootIds = Enumerable.Range(0, rootCount)
                .Select(i => string.Format("root{0}", i))
                .ToArray();

            var rootIdIndex = 0;

            Console.WriteLine("Generating {0} events distributed among {1} roots...", eventCount, rootCount);
            QuicklyGenerateLoadsOfEvents(eventCount, () =>
            {
                var id = rootIds[rootIdIndex];
                rootIdIndex = (rootIdIndex + 1)%rootIds.Length;
                return id;
            });

            var counter = 0L;
            Action print = () =>
            {
                Console.WriteLine("Iterated through {0} events... (ram: {1:0.0})", Interlocked.Read(ref counter), Process.GetCurrentProcess().WorkingSet64 / 1000000);
            };

            TakeTime(string.Format("load {0}/{1} = {2} events", eventCount, rootCount, eventCount/rootCount), () =>
            {
                using (var timer = new Timer(5000))
                {
                    timer.Elapsed += delegate { print(); };
                    timer.Start();
                    var randoom = new Random();

                    1000.Times(() =>
                    {
                        foreach (var e in _eventStore.Load(rootIds[randoom.Next(rootIds.Length)]))
                        {
                            Interlocked.Increment(ref counter);
                        }
                    });
                }
            });

            print();
        }

        void QuicklyGenerateLoadsOfEvents(int eventCount, Func<string> aggregateRootIdFactory = null)
        {
            aggregateRootIdFactory = aggregateRootIdFactory ?? (() => "rootid");

            var events = Enumerable.Range(0, eventCount)
                .Select(number =>
                {
                    var batchId = Guid.NewGuid().ToString();

                    return new MongoEventBatch
                    {
                        BatchId = batchId,
                        Events = new List<MongoEvent>
                        {
                            new MongoEvent
                            {
                                AggregateRootId = aggregateRootIdFactory(),
                                GlobalSequenceNumber = number,
                                SequenceNumber = number,
                                Meta = new Dictionary<string, string>
                                {
                                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, number.ToString(Metadata.NumberCulture)},
                                    {DomainEvent.MetadataKeys.SequenceNumber, number.ToString(Metadata.NumberCulture)},
                                    {DomainEvent.MetadataKeys.AggregateRootId, "rootid"},
                                    {DomainEvent.MetadataKeys.TimeUtc, DateTime.UtcNow.ToString("u")},
                                    {DomainEvent.MetadataKeys.BatchId, batchId},
                                },
                                Bin = Encoding.UTF8.GetBytes("jieojbieow jiboe wbijeo wjbio jbieow jbioe wjbioe wjibej wio bjeiwob")
                            }
                        }
                    };
                });

            var collection = _mongoDatabase.GetCollection<MongoEventBatch>("Events");

            collection.InsertBatch(events);
        }

        class MongoEventBatch
        {
            [BsonId]
            public string BatchId { get; set; }

            public List<MongoEvent> Events { get; set; }
        }

        class MongoEvent
        {
            public Dictionary<string, string> Meta { get; set; }
            public byte[] Bin { get; set; }
            public string Body { get; set; }

            public long GlobalSequenceNumber { get; set; }
            public long SequenceNumber { get; set; }
            public string AggregateRootId { get; set; }
        }

        public class SomeDomainEvent : DomainEvent
        {
            public string Text { get; set; }
            public long GlobalSequenceNumber { get; set; }
            public long SequenceNumber { get; set; }
            public string AggregateRootId { get; set; }
        }
    }
}