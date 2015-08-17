using System;
using System.Threading;
using d60.Cirqus.Extensions;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Locators;

namespace d60.Cirqus.Tests.Views.Distribution.Model
{
    public class SomeRootView : IViewInstance<InstancePerAggregateRootLocator>, ISubscribeTo<DidStuff>
    {
        static int _threadIdCounter;

        public string Id { get; set; }
        public long LastGlobalSequenceNumber { get; set; }
        public int HowManyThingsDidItDo { get; set; }
        public void Handle(IViewContext context, DidStuff domainEvent)
        {
            if (Thread.CurrentThread.Name == null)
            {
                Thread.CurrentThread.Name = string.Format("thread-{0}", Interlocked.Increment(ref _threadIdCounter));
            }

            Thread.Sleep(1000);
            
            HowManyThingsDidItDo ++;

            Console.WriteLine("{0}: {1}", Thread.CurrentThread.Name, domainEvent.GetGlobalSequenceNumber());
        }
    }
}