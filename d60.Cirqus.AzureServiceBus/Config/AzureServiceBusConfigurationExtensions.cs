using d60.Cirqus.Config.Configurers;
using d60.Cirqus.Events;
using d60.Cirqus.Projections;

namespace d60.Cirqus.AzureServiceBus.Config
{
    public static class AzureServiceBusConfigurationExtensions
    {
        public static void UseAzureServiceBus(this EventDispatcherConfigurationBuilder builder, string connectionString, string topicName)
        {
            builder.Registrar.Register<IEventDispatcher>(context => new AzureServiceBusEventDispatcherSender(connectionString, topicName));
        }
    }
}