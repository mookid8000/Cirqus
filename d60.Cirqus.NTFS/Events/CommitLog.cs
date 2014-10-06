using System;
using System.IO;
using System.Security.AccessControl;
using System.Text;

namespace d60.Cirqus.NTFS.Events
{
    public class CommitLog : IDisposable
    {
        public const int SizeofCommitRecord = sizeof(long);

        readonly BinaryWriter _writer;
        readonly BinaryReader _reader;

        public CommitLog(string basePath, bool dropEvents)
        {
            var commitsFilePath = Path.Combine(basePath, "commits.idx");

            if (dropEvents && File.Exists(commitsFilePath)) 
                File.Delete(commitsFilePath);

            _writer = new BinaryWriter(
                new FileStream(commitsFilePath, FileMode.Append, FileSystemRights.AppendData, FileShare.Read, 1024, FileOptions.None),
                Encoding.ASCII, leaveOpen: false);

            _reader = new BinaryReader(
                new FileStream(commitsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024, FileOptions.None), 
                Encoding.ASCII, leaveOpen: false);
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

            var garbage = currentLength % SizeofCommitRecord;
            if (garbage > 0)
            {
                // we have a failed commit on our hands, skip the garbage
                isCorrupted = true;
                _reader.BaseStream.Seek(-garbage, SeekOrigin.Current);
            }

            // read commit and checksum
            if (currentLength < garbage + SizeofCommitRecord * 2) return -1;
            _reader.BaseStream.Seek(-SizeofCommitRecord * 2, SeekOrigin.Current);
            var globalSequenceNumber = _reader.ReadInt64();
            var checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            // ok, the checksum was never written, skip the orphaned commit and try again
            isCorrupted = true;
            if (currentLength < garbage + SizeofCommitRecord * 3) return -1;
            _reader.BaseStream.Seek(-SizeofCommitRecord * 3, SeekOrigin.Current);
            globalSequenceNumber = _reader.ReadInt64();
            checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            throw new InvalidOperationException("Commit file is unreadable.");
        }

        public void Recover(long lastKnownGoodCommit)
        {
            _writer.BaseStream.SetLength(lastKnownGoodCommit * SizeofCommitRecord);
        }

        public void Dispose()
        {
            Writer.Dispose();
            _reader.Dispose();
        }
    }
}