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
            builder.RegisterInstance<Action<TestContext>>(o => o.Asynchronous = true, multi: true);
        }
    }
}