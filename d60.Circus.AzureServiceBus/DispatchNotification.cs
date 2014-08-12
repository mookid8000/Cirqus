using System;

namespace d60.Circus.AzureServiceBus
{
    [Serializable]
    class DispatchNotification
    {
        public string DomainEvents { get; set; }
    }
}