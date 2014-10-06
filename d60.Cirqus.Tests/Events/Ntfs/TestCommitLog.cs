using d60.Cirqus.NTFS.Events;
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
            var global = _log.Read();
            Assert.AreEqual(-1, global);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromOkFile()
        {
            _log.Writer.Write(10L);
            _log.Writer.Write(10L);
            _log.Writer.Flush();

            var global = _log.Read();
            Assert.AreEqual(10, global);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptFirstCommit()
        {
            // a corrupted one
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            var global = _log.Read();
            Assert.AreEqual(-1, global);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithCorruptFirstChecksum()
        {
            // a corrupted one
            _log.Writer.Write(0L);
            _log.Writer.Write((byte)0);
            _log.Writer.Flush();

            var global = _log.Read();
            Assert.AreEqual(-1, global);
        }

        [Test]
        public void GetLastComittedGlobalSequenceNumberFromFileWithMissingFirstChecksum()
        {
            // a commit without checksum
            _log.Writer.Write(0L);
            _log.Writer.Flush();

            var global = _log.Read();
            Assert.AreEqual(-1, global);
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

            var global = _log.Read();
            Assert.AreEqual(10, global);
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

            var global = _log.Read();
            Assert.AreEqual(10, global);
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

            var global = _log.Read();
            Assert.AreEqual(10, global);
        }
    }
}