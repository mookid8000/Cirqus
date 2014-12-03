using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Serialization;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events
{
    [TestFixture]
    public class TestEventCache
    {
        TrackingEventStore store;

        [SetUp]
        public void Setup()
        {
            store = new TrackingEventStore(
                new InMemoryEventStore(new JsonDomainEventSerializer()));
        }

        [Test]
        public void LoadsEvents()
        {
            Save("(H)aggrid", 0);
            
            var cache = new EventCache(store);
            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(results, store.CacheMisses);
        }

        [Test]
        public void LoadsFromCache()
        {
            Save("(H)aggrid", 0);
            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var cache = new EventCache(store);
            var _ = cache.Load("(H)aggrid").ToList();
            store.CacheMisses.Clear();
            
            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(DataForSeq(1), results[1].Data);
            Assert.AreEqual(DataForSeq(2), results[2].Data);
            Assert.AreEqual(0, store.CacheMisses.Count);
        }

        [Test]
        public void LoadsFromLatePartiallyCachedStream()
        {
            Save("(H)aggrid", 0);
            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var cache = new EventCache(store);
            var _ = cache.Load("(H)aggrid", 1).ToList();
            store.CacheMisses.Clear();
            
            var results = cache.Load("(H)aggrid").ToList();

            //TODO
            //Assert.AreEqual(DataForSeq(0), results[0].Data);
            //Assert.AreEqual(DataForSeq(1), results[1].Data);
            //Assert.AreEqual(DataForSeq(2), results[2].Data);
            //Assert.AreEqual(1, store.CacheMisses.Count);
            //Assert.AreEqual(DataForSeq(0), store.CacheMisses[0].Data);
        }

        [Test]
        public void LoadsFromEarlyPartiallyCachedStream()
        {
            Save("(H)aggrid", 0);

            var cache = new EventCache(store);
            var _ = cache.Load("(H)aggrid").ToList();
            store.CacheMisses.Clear();

            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(DataForSeq(1), results[1].Data);
            Assert.AreEqual(DataForSeq(2), results[2].Data);
            Assert.AreEqual(2, store.CacheMisses.Count);
            Assert.AreEqual(DataForSeq(1), store.CacheMisses[0].Data);
            Assert.AreEqual(DataForSeq(2), store.CacheMisses[1].Data);
        }

        void Save(string id, long seq)
        {
            var domainEvent = new TheDomainEvent();
            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = id;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = seq.ToString();

            store.Save(Guid.NewGuid(), new[]
            {
                EventData.FromDomainEvent(domainEvent, DataForSeq(seq))
            });
        }

        static byte[] DataForSeq(long seq)
        {
            return BitConverter.GetBytes(seq);
        }

        public class TrackingEventStore : IEventStore
        {
            readonly IEventStore store;

            public TrackingEventStore(IEventStore store)
            {
                this.store = store;

                CacheMisses = new List<EventData>();
            }

            public List<EventData> CacheMisses { get; private set; }

            public void Save(Guid batchId, IEnumerable<EventData> batch)
            {
                store.Save(batchId, batch);
            }

            public IEnumerable<EventData> Load(string aggregateRootId, long firstSeq = 0)
            {
                var events = store.Load(aggregateRootId, firstSeq).ToList();

                CacheMisses.AddRange(events);
                
                return events;
            }

            public IEnumerable<EventData> Stream(long globalSequenceNumber = 0)
            {
                throw new NotImplementedException();
            }

            public long GetNextGlobalSequenceNumber()
            {
                throw new NotImplementedException();
            }
        }

        public class TheDomainEvent : DomainEvent
        {
            
        }
    }
}