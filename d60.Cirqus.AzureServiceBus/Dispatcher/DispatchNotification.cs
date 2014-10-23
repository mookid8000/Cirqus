using System;

namespace d60.Cirqus.AzureServiceBus.Dispatcher
{
    [Serializable]
    class DispatchNotification
    {
        public string DomainEvents { get; set; }
    }
}