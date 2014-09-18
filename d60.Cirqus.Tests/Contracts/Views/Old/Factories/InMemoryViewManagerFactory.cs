using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.Old;

namespace d60.Cirqus.Tests.Contracts.Views.Old.Factories
{
    public class InMemoryViewManagerFactory : IPushViewManagerFactory
    {
        readonly List<IPushViewManager> _viewManagers = new List<IPushViewManager>();

        public IPushViewManager GetPushViewManager<TView>() where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var viewManager = new Cirqus.Views.ViewManagers.Old.InMemoryViewManager<TView>();
            _viewManagers.Add(viewManager);
            return new PushOnlyWrapper(viewManager);
        }

        public TView Load<TView>(string viewId) where TView : class, IViewInstance, ISubscribeTo, new()
        {
            var matchingViewManagers = _viewManagers.OfType<Cirqus.Views.ViewManagers.Old.InMemoryViewManager<TView>>().ToList();

            if (matchingViewManagers.Count != 1)
            {
                var message = string.Format("Expected to find exactly 1 in-mem view manager for {0}, but found" +
                                            " {1} - when testing in-mem view managers, there must be exactly" +
                                            " one for each type of view that is to be tested",
                    typeof (TView), matchingViewManagers.Count);

                throw new InvalidOperationException(message);
            }

            return matchingViewManagers[0].Load(viewId);
        }

        public void SetMaxDomainEventsBetweenFlush(int value)
        {
        }
    }
}