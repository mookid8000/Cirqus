using System;
using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Views.ViewManagers;
using d60.Cirqus.Views.ViewManagers.New;

namespace d60.Cirqus.Tests.Contracts.Views.New.Factories
{
    public abstract class ManagedViewFactoryBase
    {
        protected readonly List<IManagedView> ManagedViews = new List<IManagedView>();

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

        public abstract IManagedView<TViewInstance> CreateManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new(); 

        protected IManagedView<TViewInstance> GetManagedView<TViewInstance>() where TViewInstance : class, IViewInstance, ISubscribeTo, new()
        {
            var managedView = ManagedViews
                .OfType<IManagedView<TViewInstance>>()
                .FirstOrDefault();

            if (managedView == null)
            {
                throw new ArgumentException(string.Format("Could not find managed view for {0} - only have {1}",
                    typeof(TViewInstance), string.Join(", ", ManagedViews)));
            }

            return managedView;
        }

    }
}