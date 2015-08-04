using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.MongoDb.Views;
using d60.Cirqus.Tests.MongoDb;
using d60.Cirqus.Views;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Views.Distribution
{
    [TestFixture]
    public class TestMongoDbAutoDistributionPersistence : FixtureBase
    {
        MongoDbAutoDistributionState _state;

        protected override void DoSetUp()
        {
            _state = new MongoDbAutoDistributionState(MongoHelper.InitializeTestDatabase(), "AutoDistribution");
        }

        [Test]
        public void CanEmitHeartbeats()
        {
            _state.Heartbeat("1", true);
            _state.Heartbeat("1", true);
            _state.Heartbeat("1", true);
            _state.Heartbeat("1", true);

            _state.Heartbeat("2", true);
            _state.Heartbeat("2", true);
            _state.Heartbeat("2", true);

            _state.Heartbeat("3", true);
            _state.Heartbeat("3", false);
        }

        [Test]
        public void CanGetAvailableIds()
        {
            _state.Heartbeat("1", true);
            _state.Heartbeat("2", true);
            _state.Heartbeat("3", false);
            _state.Heartbeat("4", true);

            var currentState = _state.GetCurrentState()
                .Select(kvp => kvp.ManagerId)
                .OrderBy(id => id);

            Assert.That(currentState.ToArray(), Is.EqualTo(new[] { "1", "2", "4" }));
        }

        [Test]
        public void CanGetCurrentState()
        {
            _state.Heartbeat("1", true);

            var currentState = _state.GetCurrentState();

            Assert.That(GetViewIds(currentState, "1"), Is.EqualTo(Enumerable.Empty<string>()));
        }

        [Test]
        public void CanSetCurrentState()
        {
            _state.Heartbeat("1", true);
            _state.Heartbeat("2", true);
            _state.Heartbeat("3", true);

            /*
            new Dictionary<string, HashSet<string>>
            {
                {"1", new HashSet<string>{"view1", "view2"}},
                {"2", new HashSet<string>{"view3", "view4"}},
                {"3", new HashSet<string>{"view5", "view6"}},
            }
            */

            _state.SetNewState(new List<AutoDistributionState>
            {
                new AutoDistributionState("1", new[] {"view1", "view2"}),
                new AutoDistributionState("2", new[] {"view3", "view4"}),
                new AutoDistributionState("3", new[] {"view5", "view6"}),
            });

            var currentState = _state.GetCurrentState();

            Assert.That(GetViewIds(currentState, "1"), Is.EqualTo(new[] { "view1", "view2" }));
            Assert.That(GetViewIds(currentState, "2"), Is.EqualTo(new[] { "view3", "view4" }));
            Assert.That(GetViewIds(currentState, "3"), Is.EqualTo(new[] { "view5", "view6" }));
        }

        static HashSet<string> GetViewIds(IEnumerable<AutoDistributionState> currentState, string managerId)
        {
            return currentState.First(s => s.ManagerId == managerId).ViewIds;
        }
    }
}