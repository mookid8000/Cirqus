using System;
using System.Collections.Generic;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Views;
using d60.Cirqus.Views.ViewManagers;

namespace d60.Cirqus.Testing
{
    /// <summary>
    /// Configuration builder that allows for configuring a <see cref="ViewManagerEventDispatcher"/>
    /// </summary>
    public class SynchronousViewManagerEventDispatcherConfiguationBuilder : ConfigurationBuilder<SynchronousViewManagerEventDispatcher>
    {
        /// <summary>
        /// Creates the builder
        /// </summary>
        public SynchronousViewManagerEventDispatcherConfiguationBuilder(IRegistrar registrar) : base(registrar) { }

        /// <summary>
        /// Makes the given dictionary of items available in the <see cref="IViewContext"/> passed to the view's
        /// locator and the view itself
        /// </summary>
        public SynchronousViewManagerEventDispatcherConfiguationBuilder WithViewContext(IDictionary<string, object> viewContextItems)
        {
            if (viewContextItems == null) throw new ArgumentNullException("viewContextItems");
            RegisterInstance(viewContextItems);
            return this;
        }
    }
}