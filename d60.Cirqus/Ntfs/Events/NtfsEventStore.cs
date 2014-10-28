using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Numbers;

namespace d60.Cirqus.Ntfs.Events
{
    /// <summary>
    /// Event store that saves events directly to files for use in embedded scenarios. 
    /// Only supports a single process using the store at a time. 
    /// Uses filesystem's index for easy lookup of single aggregate streams.
    /// Uses a single file for global sequence index.
    /// Uses a single file for commit log.
    /// </summary>
    public class NtfsEventStore : IEventStore, IDisposable
    {
        readonly object _lock = new object();

        public NtfsEventStore(string basePath) : this(basePath, dropEvents: false) {}

        internal NtfsEventStore(string basePath, bool dropEvents)
        {
            Directory.CreateDirectory(basePath);

            GlobalSequenceIndex = new GlobalSequenceIndex(basePath, dropEvents);
            DataStore = new DataStore(basePath, dropEvents);
            CommitLog = new CommitLog(basePath, dropEvents);
        }

        internal GlobalSequenceIndex GlobalSequenceIndex { get; private set; }
        internal DataStore DataStore { get; private set; }
        internal CommitLog CommitLog { get; private set; }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<DomainEvent> Load(Guid aggregateRootId, long firstSeq = 0)
        {
           throw new NotImplementedException();
        }

        public IEnumerable<DomainEvent> Stream(long globalSequenceNumber = 0)
        {
            throw new NotImplementedException();
        }

        public long GetNextGlobalSequenceNumber()
        {
            return CommitLog.Read() + 1;
        }

        public void Save(Guid batchId, IEnumerable<Event> events)
        {
            lock (_lock)
            {
                var list = events.ToList();

                bool isCorrupted;
                var globalSequenceNumber = CommitLog.Read(out isCorrupted);
                if (isCorrupted) CommitLog.Recover();

                GlobalSequenceIndex.DetectCorruptionAndRecover(DataStore, globalSequenceNumber);

                foreach (var domainEvent in list)
                {
                    domainEvent.Meta[DomainEvent.MetadataKeys.GlobalSequenceNumber] = (++globalSequenceNumber).ToString(Metadata.NumberCulture);
                    domainEvent.Meta[DomainEvent.MetadataKeys.BatchId] = batchId.ToString();
                }

                EventValidation.ValidateBatchIntegrity(batchId, list);

                GlobalSequenceIndex.Write(list);
                DataStore.Write(batchId, list);
                CommitLog.Write(globalSequenceNumber);
            }
        }

        public IEnumerable<Event> LoadNew(Guid aggregateRootId, long firstSeq = 0)
        {
            var lastCommittedGlobalSequenceNumber = CommitLog.Read();

            return DataStore.Read(lastCommittedGlobalSequenceNumber, aggregateRootId, firstSeq);
        }

        public IEnumerable<Event> StreamNew(long globalSequenceNumber = 0)
        {
            var lastCommittedGlobalSequenceNumber = CommitLog.Read();

            return from record in GlobalSequenceIndex.Read(lastCommittedGlobalSequenceNumber, offset: globalSequenceNumber)
                   select DataStore.Read(record.AggregateRootId, record.LocalSequenceNumber);
        }

        public void Dispose()
        {
            GlobalSequenceIndex.Dispose();
            CommitLog.Dispose();
        }
    }
}
