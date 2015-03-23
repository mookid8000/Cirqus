using System;
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
        readonly ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, TimeSpan>> 
            _timeSpent = new ConcurrentDictionary<IViewManager, ConcurrentDictionary<Type, TimeSpan>>();

        public void RegisterTimeSpent(IViewManager viewManager, DomainEvent domainEvent, TimeSpan duration)
        {
            _timeSpent.GetOrAdd(viewManager, vm => new ConcurrentDictionary<Type, TimeSpan>())
                .AddOrUpdate(domainEvent.GetType(), type => duration, (type, sum) => sum + duration);
        }

        public ViewManagerStatsResult GetStats()
        {
            var viewManagers = _timeSpent.Keys.ToList();
            var collectedStats = new List<ViewManagerStats>();

            foreach (var viewManager in viewManagers)
            {
                ConcurrentDictionary<Type, TimeSpan> dict;
                if (!_timeSpent.TryRemove(viewManager, out dict)) continue;

                collectedStats.Add(new ViewManagerStats(viewManager, dict.Select(kvp => new DomainEventStats(kvp.Key, kvp.Value))));
            }

            return new ViewManagerStatsResult(collectedStats);
        }

        public class ViewManagerStatsResult
        {
            public ViewManagerStatsResult(List<ViewManagerStats> stats)
            {
                Stats = stats;
            }

            public List<ViewManagerStats> Stats { get; private set; }
        }

        public class ViewManagerStats
        {
            public ViewManagerStats(IViewManager viewManager, IEnumerable<DomainEventStats> eventStats)
            {
                ViewManager = viewManager;
                Stats = eventStats.ToList();
            }

            public IViewManager ViewManager { get; private set; }
            
            public List<DomainEventStats> Stats { get; private set; }

            public override string ToString()
            {
                return string.Format(@"{0}:
{1}", ViewManager.GetType().GetPrettyName(), string.Join(Environment.NewLine, Stats.Select(s => s.ToString()).Indented()));
            }
        }

        public class DomainEventStats
        {
            public DomainEventStats(Type domainEventType, TimeSpan elapsed)
            {
                DomainEventType = domainEventType;
                Elapsed = elapsed;
            }

            public Type DomainEventType { get; private set; }
            
            public TimeSpan Elapsed { get; private set; }

            public override string ToString()
            {
                return string.Format("{0}: {1:0.0} s", DomainEventType.Name, Elapsed.TotalSeconds);
            }
        }
    }
}