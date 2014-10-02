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
            _eventStore = new NtfsEventStore("testdata", dropEvents: true);
        }
    }
}