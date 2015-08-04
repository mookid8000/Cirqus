using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.Distribution
{
    [TestFixture]
    public class TestMongoDbAutoDistributionPersistence : FixtureBase
    {
        MongoDbAutoDistributionPersistence _persistence;

        protected override void DoSetUp()
        {
            _persistence = new MongoDbAutoDistributionPersistence(MongoHelper.InitializeTestDatabase(), "AutoDistribution");
        }

        [Test]
        public void CanEmitHeartbeats()
        {
            _persistence.Heartbeat("1", true);
            _persistence.Heartbeat("1", true);
            _persistence.Heartbeat("1", true);
            _persistence.Heartbeat("1", true);
            
            _persistence.Heartbeat("2", true);
            _persistence.Heartbeat("2", true);
            _persistence.Heartbeat("2", true);
            
            _persistence.Heartbeat("3", true);
            _persistence.Heartbeat("3", false);
        }

        [Test]
        public void CanGetAvailableIds()
        {
            _persistence.Heartbeat("1", true);
            _persistence.Heartbeat("2", true);
            _persistence.Heartbeat("3", false);
            _persistence.Heartbeat("4", true);

            var currentState = _persistence.GetCurrentState()
                .Select(kvp => kvp.Key)
                .OrderBy(id => id);

            Assert.That(currentState.ToArray(), Is.EqualTo(new[]{"1", "2", "4"}));
        }

        [Test]
        public void CanGetCurrentState()
        {
            _persistence.Heartbeat("1", true);

            var currentState = _persistence.GetCurrentState();

            Assert.That(currentState["1"], Is.EqualTo(Enumerable.Empty<string>()));
        }

        [Test]
        public void CanSetCurrentState()
        {
            _persistence.Heartbeat("1", true);
            _persistence.Heartbeat("2", true);
            _persistence.Heartbeat("3", true);

            _persistence.SetNewState(new Dictionary<string, HashSet<string>>
            {
                {"1", new HashSet<string>{"view1", "view2"}},
                {"2", new HashSet<string>{"view3", "view4"}},
                {"3", new HashSet<string>{"view5", "view6"}},
            });

            var currentState = _persistence.GetCurrentState();

            Assert.That(currentState["1"], Is.EqualTo(new[]{"view1", "view2"}));
            Assert.That(currentState["2"], Is.EqualTo(new[]{"view3", "view4"}));
            Assert.That(currentState["3"], Is.EqualTo(new[]{"view5", "view6"}));
        }
    }
}