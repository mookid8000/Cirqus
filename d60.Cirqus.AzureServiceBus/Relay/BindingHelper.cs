using System;
using System.ServiceModel.Channels;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    public class BindingHelper
    {
        public static Binding CreateBinding()
        {
            return new NetTcpRelayBinding
            {
                MaxReceivedMessageSize = int.MaxValue,
                CloseTimeout = TimeSpan.FromSeconds(10),
                OpenTimeout = TimeSpan.FromSeconds(10),
                ReceiveTimeout = TimeSpan.FromSeconds(10),
                SendTimeout = TimeSpan.FromSeconds(10),
                ConnectionMode = TcpRelayConnectionMode.Hybrid,
            };
        }
    }
}