using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
using d60.Cirqus.Numbers;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using NUnit.Framework;

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

        [TestCase(10000)]
        [TestCase(1000)]
        [TestCase(1000000, Ignore = TestCategories.IgnoreLongRunning)]
        [TestCase(10000000, Ignore = TestCategories.IgnoreLongRunning)]
        public void VerifyLazinessOfStreamingApi(int eventCount)
        {
            Console.Write("Generating {0} events... ", eventCount);
            QuicklyGenerateLoadsOfEvents(eventCount);
            Console.WriteLine(" Done!");


            var counter = 0;

            Action print = () =>
            {
                Console.WriteLine("Iterated through {0} events... (ram: {1:0.0})", counter, Process.GetCurrentProcess().WorkingSet64/1000000);
            };

            using (var timer = new Timer(5000))
            {
                timer.Elapsed += delegate { print(); };
                timer.Start();

                foreach (var e in _eventStore.Stream())
                {
                    counter++;
                }
            }

            print();
        }

        void QuicklyGenerateLoadsOfEvents(int eventCount)
        {
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
                                AggregateRootId = "rootid",
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