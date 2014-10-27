using System.ServiceModel.Channels;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    public class BindingHelper
    {
        public static Binding CreateDefaultRelayBinding()
        {
            return new NetTcpRelayBinding { MaxReceivedMessageSize = int.MaxValue };
        }
    }
}