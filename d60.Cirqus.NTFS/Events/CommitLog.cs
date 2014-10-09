using System;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace d60.Cirqus.NTFS.Events
{
    /// <summary>
    /// Writes last global sequence number of each batch to a log with a checksum. A full record indicates a successful commit of a full batch.
    /// Reading is thread safe and can be done concurrently with writes. Writes/Recovers must be sequential.
    /// </summary>
    internal class CommitLog : IDisposable
    {
        public const int SizeofCommit = sizeof(long);
        public const int SizeofCommitAndChecksumRecord = SizeofCommit * 2;

        readonly string _commitsFilePath;
        
        BinaryWriter _writer;
        BinaryReader _reader;

        public CommitLog(string basePath, bool dropEvents)
        {
            _commitsFilePath = Path.Combine(basePath, "commits.idx");

            if (dropEvents && File.Exists(_commitsFilePath)) 
                File.Delete(_commitsFilePath);

            OpenWriter();
            OpenReader();
        }

        public BinaryWriter Writer
        {
            get { return _writer; }
        }

        public void Write(long globalSequenceNumber)
        {
            Writer.Write(globalSequenceNumber);
            Writer.Write(globalSequenceNumber); // "checksum"
            Writer.Flush();
        }

        public long Read()
        {
            bool isCorrupted;
            return Read(out isCorrupted);
        }

        public long Read(out bool isCorrupted)
        {
            isCorrupted = false;

            var currentLength = _reader.BaseStream.Length;

            if (currentLength == 0) return -1;

            _reader.BaseStream.Seek(currentLength, SeekOrigin.Begin);

            var garbage = currentLength % SizeofCommit;
            if (garbage > 0)
            {
                // we have a failed commit on our hands, skip the garbage
                isCorrupted = true;
                _reader.BaseStream.Seek(-garbage, SeekOrigin.Current);
            }

            // there's no full commit
            if (currentLength < garbage + SizeofCommitAndChecksumRecord)
            {
                isCorrupted = true;
                return -1;
            }

            // read commit and checksum
            _reader.BaseStream.Seek(-SizeofCommitAndChecksumRecord, SeekOrigin.Current);
            var globalSequenceNumber = _reader.ReadInt64();
            var checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            // ok, the checksum was wrong, try skip the orphaned commit and try again
            isCorrupted = true;
            
            // there's no commit before this
            if (currentLength < garbage + SizeofCommitAndChecksumRecord + SizeofCommit) return -1;

            _reader.BaseStream.Seek(-(SizeofCommitAndChecksumRecord + SizeofCommit), SeekOrigin.Current);
            globalSequenceNumber = _reader.ReadInt64();
            checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            throw new InvalidOperationException("Commit file is unreadable.");
        }

        public void Recover(long lastKnownGoodCommit)
        {
            _writer.Dispose();

            using (var stream = new FileStream(_commitsFilePath, FileMode.Open, FileSystemRights.Write, FileShare.Read, 1024, FileOptions.None))
            {
                var numberOfRecords = lastKnownGoodCommit + 1;
                stream.SetLength(numberOfRecords * SizeofCommitAndChecksumRecord);
            }

            OpenWriter();
        }

        void OpenWriter()
        {
            _writer = new BinaryWriter(
                new FileStream(_commitsFilePath, FileMode.Append, FileSystemRights.AppendData, FileShare.Read, 1024, FileOptions.None),
                Encoding.ASCII, leaveOpen: false);
        }

        void OpenReader()
        {
            _reader = new BinaryReader(
                new FileStream(_commitsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.None),
                Encoding.ASCII, leaveOpen: false);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _reader.Dispose();
        }
    }
}