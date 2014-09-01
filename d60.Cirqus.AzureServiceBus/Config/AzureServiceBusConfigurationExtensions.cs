using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;

namespace d60.Cirqus.AzureServiceBus.Config
{
    public static class AzureServiceBusConfigurationExtensions
    {
        public static void UseAzureServiceBus(this EventDispatcherConfigurationBuilder builder, string connectionString, string topicName)
        {
            builder.Registrar.Register<IEventDispatcher>(() => new AzureServiceBusEventDispatcherSender(connectionString, topicName));
        }
    }
}