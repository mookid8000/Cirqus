using System;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.NTFS.Events;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Ntfs
{
    [TestFixture]
    public class TestNtfsEventStore : FixtureBase
    {
        NtfsEventStore _eventStore;

        protected override void DoSetUp()
        {
            _eventStore = RegisterForDisposal(new NtfsEventStore("testdata", dropEvents: true));
        }

        [Test]
        public void OnlyReadCommittedOnLoad()
        {
            var rootId = Guid.NewGuid();

            // make one full commit
            _eventStore.Save(rootId, new[]
            {
                new SomeEvent
                {
                    Meta =
                    {
                        {DomainEvent.MetadataKeys.SequenceNumber, 0},
                        {DomainEvent.MetadataKeys.AggregateRootId, rootId}
                    }
                }
            });

            // save an event to a file, without committing
            _eventStore.DataStore.Write(
                new SomeEvent
                {
                    Meta =
                    {
                        {DomainEvent.MetadataKeys.SequenceNumber, 1},
                        {DomainEvent.MetadataKeys.AggregateRootId, rootId},
                        {DomainEvent.MetadataKeys.GlobalSequenceNumber, 1}
                    }
                });


            var events = _eventStore.Load(rootId);
            Assert.AreEqual(1, events.Count());
        }

        [Test]
        public void OnlyReadCommittedOnStream()
        {
            var rootId = Guid.NewGuid();

            // make one full commit
            _eventStore.Save(rootId, new[]
            {
                new SomeEvent
                {
                    Meta =
                    {
                        {DomainEvent.MetadataKeys.SequenceNumber, 0},
                        {DomainEvent.MetadataKeys.AggregateRootId, rootId}
                    }
                }
            });

            // save an event to sequence-index, without committing
            _eventStore.GlobalSequenceIndex.Write(new[]
            {
                new GlobalSequenceIndex.GlobalSequenceRecord
                {
                    GlobalSequenceNumber = 1,
                    AggregateRootId = rootId,
                    LocalSequenceNumber = 1
                }
            });

            var events = _eventStore.Stream();
            Assert.AreEqual(1, events.Count());
        }

        class SomeEvent : DomainEvent
        {
        }
    }
}