using d60.Cirqus.AzureServiceBus.Dispatcher;
using d60.Cirqus.AzureServiceBus.Relay;
using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Views;

namespace d60.Cirqus.AzureServiceBus.Config
{
    public static class AzureServiceBusConfigurationExtensions
    {
        public static void UseAzureServiceBusEventDispatcher(this EventDispatcherConfigurationBuilder builder, string connectionString, string topicName)
        {
            builder.Registrar.Register<IEventDispatcher>(context => new AzureServiceBusEventDispatcherSender(connectionString, topicName));
        }

        /// <summary>
        /// Installs an event dispatcher that can be contacted from anywhere
        /// </summary>
        public static void UseAzureServiceBusRelayEventDispatcher(this EventDispatcherConfigurationBuilder builder, string serviceNamespace, string servicePath, string keyName, string sharesAccessKey)
        {
            builder.Registrar.Register<IEventDispatcher>(context =>
            {
                var eventStore = context.Get<IEventStore>();

                return new AzureServiceBusRelayEventDispatcher(eventStore, serviceNamespace, servicePath, keyName, sharesAccessKey);
            });
        }
    }
}