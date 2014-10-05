using System;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.NTFS.Events;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events
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
        public void GetNextGlobalSequenceNumberFromEmptyFile()
        {
            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(0, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromOkFile()
        {
            _eventStore.CommitsWriter.Write(10L);
            _eventStore.CommitsWriter.Write(10L);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(11, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithCorruptFirstCommit()
        {
            // a corrupted one
            _eventStore.CommitsWriter.Write((byte)0);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(0, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithCorruptFirstChecksum()
        {
            // a corrupted one
            _eventStore.CommitsWriter.Write(0L);
            _eventStore.CommitsWriter.Write((byte)0);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(0, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithMissingFirstChecksum()
        {
            // a commit without checksum
            _eventStore.CommitsWriter.Write(0L);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(0, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithCorruptCommit()
        {
            // a good one
            _eventStore.CommitsWriter.Write(10L);
            _eventStore.CommitsWriter.Write(10L);

            // a corrupted one
            _eventStore.CommitsWriter.Write((byte)11);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(11, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithCorruptChecksum()
        {
            // a good one
            _eventStore.CommitsWriter.Write(10L);
            _eventStore.CommitsWriter.Write(10L);

            // a corrupted one
            _eventStore.CommitsWriter.Write(11L);
            _eventStore.CommitsWriter.Write((byte)11);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(11, global);
        }

        [Test]
        public void GetNextGlobalSequenceNumberFromFileWithMissingChecksum()
        {
            // a good one
            _eventStore.CommitsWriter.Write(10L);
            _eventStore.CommitsWriter.Write(10L);

            // a commit without checksum
            _eventStore.CommitsWriter.Write(11L);
            _eventStore.CommitsWriter.Flush();

            var global = _eventStore.GetNextGlobalSequenceNumber();
            Assert.AreEqual(11, global);
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
            _eventStore.WriteEvent(
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
            var @event = new SomeEvent
            {
                Meta =
                {
                    {DomainEvent.MetadataKeys.SequenceNumber, 1},
                    {DomainEvent.MetadataKeys.AggregateRootId, rootId},
                    {DomainEvent.MetadataKeys.GlobalSequenceNumber, 1}
                }
            };

            _eventStore.WriteEventToSeqIndex(1, Guid.NewGuid(), @event);
            _eventStore.SeqWriter.Flush();

            var events = _eventStore.Stream();
            Assert.AreEqual(1, events.Count());
        }

        class SomeEvent : DomainEvent
        {
        }
    }
}