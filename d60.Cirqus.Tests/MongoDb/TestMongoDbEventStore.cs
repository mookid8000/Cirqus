using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using d60.Cirqus.Events;
using d60.Cirqus.MongoDb.Events;
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
                .Select(number => new
                {
                    _id = Guid.NewGuid().ToString(),

                    Events = new[]
                    {
                        new
                        {
                            _t =
                                string.Format("{0}, {1}", typeof (SomeDomainEvent).FullName,
                                    typeof (SomeDomainEvent).Assembly.GetName().Name),

                            Meta = new
                            {
                                _t = "Metadata, <events>",
                                gl_seq = number,
                                seq = number,
                                root_id = "C13CA180-4490-4397-9689-1E7D923EFD21",
                                time_utc = DateTime.UtcNow.ToString("o"),
                                batch_id = Guid.NewGuid().ToString()
                            },
                            Text = string.Format("event number {0}", number)
                        }
                    }
                });

            var collection = _mongoDatabase.GetCollection<SomeDomainEvent>("Events");

            collection.InsertBatch(events);
        }

        public class SomeDomainEvent : DomainEvent
        {
            public string Text { get; set; }
        }
    }
}