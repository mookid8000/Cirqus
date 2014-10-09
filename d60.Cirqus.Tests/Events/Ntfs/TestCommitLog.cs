using d60.Cirqus.Ntfs.Events;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Events.Ntfs
{
    [TestFixture]
    public class TestCommitLog : FixtureBase
    {
        CommitLog _log;

        protected override void DoSetUp()
        {
            _log = RegisterForDisposal(new CommitLog("testdata", dropEvents: true));
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromEmptyFile()
        {
            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromOkFile()
        {
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptFirstCommit()
        {
            // a corrupted one
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(true, corrupted);
        }

        [Test]
        public void RecoverFileWithCorruptFirstCommit()
        {
            // a corrupted one
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptFirstChecksum()
        {
            // a corrupted one
            _log.Writer.Write(0L);
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(true, corrupted);
        }

        [Test]
        public void RecoverFileWithCorruptFirstChecksum()
        {
            // a corrupted one
            _log.Writer.Write(0L);
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithMissingFirstChecksum()
        {
            // a commit without checksum
            _log.Writer.Write(0L);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(true, corrupted);
        }
        [Test]
        public void RecoverFileWithMissingFirstChecksum()
        {
            // a commit without checksum
            _log.Writer.Write(0L);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(-1, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptCommit()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a corrupted one
            _log.Writer.Write((byte)11);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(true, corrupted);
        }

        [Test]
        public void RecoverFileWithCorruptCommit()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a corrupted one
            _log.Writer.Write((byte)11);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptChecksum()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a corrupted one
            _log.Writer.Write(11L);
            _log.Writer.Write((byte)11);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(true, corrupted);
        }

        [Test]
        public void RecvoerFileWithCorruptChecksum()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a corrupted one
            _log.Writer.Write(11L);
            _log.Writer.Write((byte)11);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(false, corrupted);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithMissingChecksum()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a commit without checksum
            _log.Writer.Write(11L);
            _log.Writer.Flush();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(true, corrupted);
        }

        [Test]
        public void RecoverFileWithMissingChecksum()
        {
            // a good one
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);

            // a commit without checksum
            _log.Writer.Write(11L);
            _log.Writer.Flush();

            _log.Recover();

            bool corrupted;
            var global = _log.Read(out corrupted);
            Assert.AreEqual(10, global);
            Assert.AreEqual(false, corrupted);
        }
    }
}