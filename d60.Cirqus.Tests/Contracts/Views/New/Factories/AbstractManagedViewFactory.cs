using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public abstract class AbstractManagedViewFactory
    {
        readonly List<IManagedView> _managedViews = new List<IManagedView>();

        public TViewInstance Load<TViewInstance>(string viewId) where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = GetManagedView<TViewInstance>();

            return managedView.Load(viewId);
        }

        public void PurgeView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = GetManagedView<TViewInstance>();

            managedView.Purge();
        }

        public IManagedView<TViewInstance> GetManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = _managedViews
                .OfType<IManagedView<TViewInstance>>()
                .FirstOrDefault();

            if (managedView == null)
            {
                managedView = CreateManagedView<TViewInstance>();
                managedView.Purge();
                _managedViews.Add(managedView);
            }

            return managedView;
        }

        protected abstract IManagedView<TViewInstance> CreateManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new();
    }
}