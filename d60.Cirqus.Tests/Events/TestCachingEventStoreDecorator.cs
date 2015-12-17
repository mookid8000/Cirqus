using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Testing.Internals;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events
{
    [TestFixture, Ignore("don't have time to look at this one now...")]
    public class TestCachingEventStoreDecorator
    {
        TrackingEventStore _store;

        [SetUp]
        public void Setup()
        {
            _store = new TrackingEventStore(new InMemoryEventStore());
        }

        [Test]
        public void LoadsEvents()
        {
            Save("(H)aggrid", 0);
            
            var cache = new CachingEventStoreDecorator(_store);
            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(results, _store.CacheMisses);
        }

        [Test]
        public void LoadsFromCache()
        {
            Save("(H)aggrid", 0);
            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var cache = new CachingEventStoreDecorator(_store);
            var _ = cache.Load("(H)aggrid").ToList();
            _store.CacheMisses.Clear();
            
            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(DataForSeq(1), results[1].Data);
            Assert.AreEqual(DataForSeq(2), results[2].Data);
            Assert.AreEqual(0, _store.CacheMisses.Count);
        }

        [Test]
        public void LoadsFromLatePartiallyCachedStream()
        {
            Save("(H)aggrid", 0);
            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var cache = new CachingEventStoreDecorator(_store);
            var _ = cache.Load("(H)aggrid", 1).ToList();
            _store.CacheMisses.Clear();
            
            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(DataForSeq(1), results[1].Data);
            Assert.AreEqual(DataForSeq(2), results[2].Data);
            Assert.AreEqual(1, _store.CacheMisses.Count);
            Assert.AreEqual(DataForSeq(0), _store.CacheMisses[0].Data);
        }

        [Test]
        public void LoadsFromEarlyPartiallyCachedStream()
        {
            Save("(H)aggrid", 0);

            var cache = new CachingEventStoreDecorator(_store);
            var _ = cache.Load("(H)aggrid").ToList();
            _store.CacheMisses.Clear();

            Save("(H)aggrid", 1);
            Save("(H)aggrid", 2);

            var results = cache.Load("(H)aggrid").ToList();

            Assert.AreEqual(DataForSeq(0), results[0].Data);
            Assert.AreEqual(DataForSeq(1), results[1].Data);
            Assert.AreEqual(DataForSeq(2), results[2].Data);
            Assert.AreEqual(2, _store.CacheMisses.Count);
            Assert.AreEqual(DataForSeq(1), _store.CacheMisses[0].Data);
            Assert.AreEqual(DataForSeq(2), _store.CacheMisses[1].Data);
        }

        void Save(string id, long seq)
        {
            var domainEvent = new TheDomainEvent();
            domainEvent.Meta[DomainEvent.MetadataKeys.AggregateRootId] = id;
            domainEvent.Meta[DomainEvent.MetadataKeys.SequenceNumber] = seq.ToString();

            _store.Save(Guid.NewGuid(), new[]
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
                foreach (var @event in store.Load(aggregateRootId, firstSeq))
                {
                    CacheMisses.Add(@event);
                    yield return @event;
                }
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