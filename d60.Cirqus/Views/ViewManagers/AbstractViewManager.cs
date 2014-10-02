using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using d60.Cirqus.Events;

namespace d60.Cirqus.Views.ViewManagers
{
    /// <summary>
    /// Abstract base class with common functionality that has proven to be useful when you want to implement <see cref="IViewManager{TViewInstance}"/>
    /// </summary>
    public abstract class AbstractViewManager<TViewInstance> : IViewManager<TViewInstance> where TViewInstance : class, IViewInstance, ISubscribeTo, new()
    {
        public async Task WaitUntilProcessed(CommandProcessingResult result, TimeSpan timeout)
        {
            if (!result.EventsWereEmitted) return;

            var mostRecentGlobalSequenceNumber = result.GetNewPosition();

            var stopwatch = Stopwatch.StartNew();

            while (GetPosition(canGetFromCache: false) < mostRecentGlobalSequenceNumber)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(string.Format("View for {0} did not catch up to {1} within {2} timeout!",
                        typeof(TViewInstance), mostRecentGlobalSequenceNumber, timeout));
                }

                await Task.Delay(TimeSpan.FromMilliseconds(10));
            }
        }

        public abstract long GetPosition(bool canGetFromCache = true);

        public abstract void Dispatch(IViewContext viewContext, IEnumerable<DomainEvent> batch);

        public abstract void Purge();

        public abstract TViewInstance Load(string viewId);
        
        public event ViewInstanceUpdatedHandler<TViewInstance> Updated;

        protected void RaiseUpdatedEventFor(IEnumerable<TViewInstance> viewInstances)
        {
            var updatedEvent = Updated;
            if (updatedEvent == null) return;

            foreach (var instance in viewInstances)
            {
                updatedEvent(instance);
            }
        }
    }
}