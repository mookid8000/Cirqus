using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Simple implementation of <see cref="IViewManagerProfiler"/> that sums up totals of time spent qualified by view manager instance
    /// and domain event type. Collected stats can be had by calling <see cref="GetStats"/> which yields the collected stats and resets
    /// the profiler. The profiler is reentrant in the sense that it does not break if called concurrently, but the results of
    /// <see cref="GetStats"/> definitely make the most sense when called periodically by one single thread.
    /// </summary>
    public class StandardViewManagerProfiler : IViewManagerProfiler
    {
        class Tracking
        {
            public Tracking(TimeSpan initialDuration)
            {
                Total = initialDuration;
                Updates = 1;
            }
            public TimeSpan Total { get; private set; }
            public int Updates { get; private set; }
            public Tracking Update(TimeSpan duration)
            {
                Total += duration;
                Updates++;
                return this;
            }
        }

        readonly ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, Tracking>>
            _timeSpent = new ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, Tracking>>();

        public void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration)
        {
            _timeSpent.GetOrAdd(viewManager, vm => new ConcurrentDictionary<Type, Tracking>())
                .AddOrUpdate(domainEvent.GetType(), type => new Tracking(duration), (type, tracking) => tracking.Update(duration));
        }

        public ViewManagerStatsResult GetStats()
        {
            var viewManagers = _timeSpent.Keys.ToList();
            var collectedStats = new List<ViewManagerStats>();

            foreach (var viewManager in viewManagers)
            {
                ConcurrentDictionary<Type, Tracking> trackings;
                if (!_timeSpent.TryRemove(viewManager, out trackings)) continue;

                collectedStats.Add(new ViewManagerStats(viewManager, trackings.Select(kvp => new DomainEventStats(kvp.Key, kvp.Value.Total, kvp.Value.Updates))));
            }

            return new ViewManagerStatsResult(collectedStats);
        }

        public class ViewManagerStatsResult : IEnumerable<ViewManagerStats>
        {
            readonly List<ViewManagerStats> _stats;

            public ViewManagerStatsResult(IEnumerable<ViewManagerStats> stats)
            {
                _stats = stats.ToList();
            }

            public IEnumerator<ViewManagerStats> GetEnumerator()
            {
                return _stats.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class ViewManagerStats : IEnumerable<DomainEventStats>
        {
            readonly List<DomainEventStats> _eventStats;

            public ViewManagerStats(IViewManager viewManager, IEnumerable<DomainEventStats> eventStats)
            {
                _eventStats = eventStats.ToList();
                ViewManager = viewManager;
            }

            public IViewManager ViewManager { get; private set; }

            public IEnumerator<DomainEventStats> GetEnumerator()
            {
                return _eventStats.GetEnumerator();
            }

            public override string ToString()
            {
                return string.Format(@"{0}:
{1}", ViewManager.GetType().GetPrettyName(), string.Join(Environment.NewLine, this.Select(s => s.ToString()).Indented()));
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class DomainEventStats
        {
            public DomainEventStats(Type domainEventType, TimeSpan elapsed, int numberOfOccurrences)
            {
                DomainEventType = domainEventType;
                Elapsed = elapsed;
                NumberOfOccurrences = numberOfOccurrences;
            }

            public Type DomainEventType { get; private set; }
            
            public TimeSpan Elapsed { get; private set; }
            
            public int NumberOfOccurrences { get; private set; }

            public override string ToString()
            {
                return string.Format("{0}: {1:0.0} s ({2})", DomainEventType.Name, Elapsed.TotalSeconds, NumberOfOccurrences);
            }
        }
    }
}