using System;
using System.Collections.Concurrent;
using d60.Cirqus.Events;
using d60.Cirqus.Tests.Contracts.Views.Factories;
using d60.Cirqus.Tests.Contracts.Views.Models.ViewProfiling;
using d60.Cirqus.Tests.Stubs;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Contracts.Views
{
    [TestFixture(typeof(MongoDbViewManagerFactory), Category = TestCategories.MongoDb)]
    [TestFixture(typeof(PostgreSqlViewManagerFactory), Category = TestCategories.PostgreSql)]
    [TestFixture(typeof(MsSqlViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(EntityFrameworkViewManagerFactory), Category = TestCategories.MsSql)]
    [TestFixture(typeof(InMemoryViewManagerFactory))]
    [TestFixture(typeof(HybridDbViewManagerFactory), Category = TestCategories.MsSql)]
    public class ViewProfiling<TFactory> : FixtureBase where TFactory : AbstractViewManagerFactory, new()
    {
        TFactory _factory;
        readonly TimeSpan _acceptedTolerance = TimeSpan.FromMilliseconds(200);

        protected override void DoSetUp()
        {
            _factory = new TFactory();
        }

        [Test]
        public void ElapsedTimeIsRegisteredForEachEvent()
        {
            var viewManager = _factory.GetViewManager<View>();

            var profiler = new MyProfiler();

            viewManager.Dispatch(new ThrowingViewContext(), new DomainEvent[]
            {
                new FirstEvent {Meta = {{DomainEvent.MetadataKeys.GlobalSequenceNumber, "0"}}},
                new SecondEvent {Meta = {{DomainEvent.MetadataKeys.GlobalSequenceNumber, "1"}}},
            }, profiler);

            AssertThat(profiler.TimeSpent[viewManager][typeof (FirstEvent)], View.FirstEventSleepMilliseconds);
            AssertThat(profiler.TimeSpent[viewManager][typeof (SecondEvent)], View.SecondEventSleepMilliseconds);
        }

        void AssertThat(TimeSpan actualDuration, int approximateExpectedDurationMilliseconds)
        {
            var lowerBound = TimeSpan.FromMilliseconds(approximateExpectedDurationMilliseconds) - _acceptedTolerance;

            var upperBound = TimeSpan.FromMilliseconds(approximateExpectedDurationMilliseconds) + _acceptedTolerance;

            Assert.That(actualDuration, Is.GreaterThan(lowerBound));
            Assert.That(actualDuration, Is.LessThan(upperBound));
        }

        class MyProfiler : IViewManagerProfiler
        {
            public readonly ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, TimeSpan>> TimeSpent = new ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, TimeSpan>>();

            public void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration)
            {
                TimeSpent.GetOrAdd(viewManager, vm => new ConcurrentDictionary<Type, TimeSpan>())
                    .AddOrUpdate(domainEvent.GetType(), type => duration, (type, existing) => existing + duration);
            }
        }
    }
}