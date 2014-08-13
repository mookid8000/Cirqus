using System;

namespace d60.Cirqus.AzureServiceBus
{
    [Serializable]
    class DispatchNotification
    {
        public string DomainEvents { get; set; }
    }
}