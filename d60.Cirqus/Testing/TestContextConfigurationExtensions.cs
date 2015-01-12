using System;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Views;

namespace d60.Cirqus.Testing
{
    public static class TestContextConfigurationExtensions
    {
        /// <summary>
        /// Configures the TestContext to not wait for views to catch up
        /// </summary>
        public static void Asynchronous(this OptionsConfigurationBuilder builder)
        {
            builder.Registrar.RegisterInstance<Action<TestContext>>(o => o.Asynchronous = true, multi: true);
        }

        /// <summary>
        /// Configures the TestContext to not wait for views to catch up
        /// </summary>
        public static void MaxDomainEventsPerBatch(this OptionsConfigurationBuilder builder, int max)
        {
            builder.Registrar.RegisterInstance<Action<ViewManagerEventDispatcher>>(o => o.MaxDomainEventsPerBatch = max, multi: true);
        }
    }
}