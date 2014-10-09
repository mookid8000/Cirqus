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
        public const int SizeofHalfRecord = sizeof(long);
        public const int SizeofFullRecord = SizeofHalfRecord * 2;

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
            long isCorrupted;
            return Read(out isCorrupted);
        }

        public long Read(out bool isCorrupted)
        {
            long garbage;
            var read = Read(out garbage);
            isCorrupted = garbage > 0;
            return read;
        }

        long Read(out long garbage)
        {
            garbage = 0;

            var currentLength = _reader.BaseStream.Length;

            if (currentLength == 0) return -1;

            _reader.BaseStream.Seek(currentLength, SeekOrigin.Begin);

            garbage = currentLength % SizeofHalfRecord;
            if (garbage > 0)
            {
                // we have a failed commit on our hands, skip the garbage
                _reader.BaseStream.Seek(-garbage, SeekOrigin.Current);
            }

            if (currentLength < garbage + SizeofFullRecord)
            {
                // there's no full commit, it's all garbage
                garbage = currentLength;
                return -1;
            }

            // read commit and checksum
            _reader.BaseStream.Seek(-SizeofFullRecord, SeekOrigin.Current);
            var globalSequenceNumber = _reader.ReadInt64();
            var checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            // checksum was wrong, mark as garbage
            garbage = garbage + SizeofHalfRecord;

            if (currentLength < garbage + SizeofFullRecord)
            {
                // there's no commit before, this it's all garbage
                garbage = currentLength;
                return -1;
            }

            // try skip the orphaned commit and try again
            _reader.BaseStream.Seek(-(SizeofHalfRecord + SizeofFullRecord), SeekOrigin.Current);
            globalSequenceNumber = _reader.ReadInt64();
            checksum = _reader.ReadInt64();

            if (globalSequenceNumber == checksum)
                return globalSequenceNumber;

            throw new InvalidOperationException("Commit file is unreadable.");
        }

        public void Recover()
        {
            _writer.Dispose();

            long garbage;
            Read(out garbage);

            if (garbage == 0)
            {
                throw new InvalidOperationException(
                    "Recover must not be called if there's is no garbage. Please check that before the call.");
            }

            using (var stream = new FileStream(_commitsFilePath, FileMode.Open, FileSystemRights.Write, FileShare.Read, 1024, FileOptions.None))
            {
                stream.SetLength(stream.Length - garbage);
                stream.Flush();
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