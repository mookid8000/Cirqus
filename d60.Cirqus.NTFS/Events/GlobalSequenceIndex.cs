using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.NTFS.Events
{
    /// <summary>
    /// Writes a reference to each event with it's global sequence number and link to event file.
    /// Reading can be done concurrently. Writes/Recovers must be sequential.
    /// </summary>
    internal class GlobalSequenceIndex : IDisposable
    {
        public const int SizeofSeqRecord = 32;

        readonly string _seqFilePath;
        readonly BinaryWriter _writer;

        public GlobalSequenceIndex(string basePath, bool dropEvents)
        {
            _seqFilePath = Path.Combine(basePath, "seq.idx");

            if (dropEvents && File.Exists(_seqFilePath)) 
                File.Delete(_seqFilePath);

            _writer = new BinaryWriter(
                new FileStream(_seqFilePath, FileMode.Append, FileSystemRights.AppendData, FileShare.Read, 100 * SizeofSeqRecord, FileOptions.None),
                Encoding.ASCII, leaveOpen: false);
        }

        public void Write(IEnumerable<DomainEvent> events)
        {
            Write(from @event in events
                  select new GlobalSequenceRecord
                  {
                      GlobalSequenceNumber = @event.GetGlobalSequenceNumber(),
                      AggregateRootId = @event.GetAggregateRootId(),
                      LocalSequenceNumber = @event.GetSequenceNumber(),
                  });
        }

        public void Write(IEnumerable<GlobalSequenceRecord> records)
        {
            foreach (var record in records)
            {
                _writer.Write(record.GlobalSequenceNumber);
                _writer.Write(record.AggregateRootId.ToByteArray());
                _writer.Write(record.LocalSequenceNumber);
            }

            _writer.Flush();
        }

        public IEnumerable<GlobalSequenceRecord> Read(long lastCommittedGlobalSequenceNumber, long offset)
        {
            using (var readStream = new FileStream(_seqFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 100 * SizeofSeqRecord, FileOptions.None))
            using (var reader = new BinaryReader(readStream, Encoding.ASCII))
            {
                readStream.Seek(offset * SizeofSeqRecord, SeekOrigin.Begin);

                while (readStream.Position <= lastCommittedGlobalSequenceNumber * SizeofSeqRecord)
                {
                    var record = new GlobalSequenceRecord
                    {
                        GlobalSequenceNumber = reader.ReadInt64(),
                        AggregateRootId = new Guid(reader.ReadBytes(16)),
                        LocalSequenceNumber = reader.ReadInt64(),
                    };

                    if (record.GlobalSequenceNumber > lastCommittedGlobalSequenceNumber)
                        break;
                    
                    yield return record;
                }
            }
        }

        public void DetectCorruptionAndRecover(DataStore store, long lastCommittedGlobalSequenceNumber)
        {
            var expectedNumberOfRecords = _writer.BaseStream.Length/SizeofSeqRecord - 1;

            if (lastCommittedGlobalSequenceNumber == expectedNumberOfRecords)
                return;

            foreach (var orphan in Read(expectedNumberOfRecords, lastCommittedGlobalSequenceNumber + 1))
            {
                store.Truncate(orphan.AggregateRootId, orphan.LocalSequenceNumber);
            }
            
            _writer.BaseStream.SetLength(lastCommittedGlobalSequenceNumber * SizeofSeqRecord);
            _writer.BaseStream.Flush();
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public class GlobalSequenceRecord
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid AggregateRootId { get; set; }
            public long LocalSequenceNumber { get; set; }
        }
    }
}