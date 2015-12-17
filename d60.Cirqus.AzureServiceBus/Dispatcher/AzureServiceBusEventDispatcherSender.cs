using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Events;
using d60.Cirqus.Views;
using Microsoft.ServiceBus.Messaging;

namespace d60.Cirqus.AzureServiceBus.Dispatcher
{
    public class AzureServiceBusEventDispatcherSender : IEventDispatcher
    {
        readonly Serializer _serializer = new Serializer();
        readonly TopicClient _topicClient;

        public AzureServiceBusEventDispatcherSender(string connectionString, string topicName)
        {
            AzureHelpers.EnsureTopicExists(connectionString, topicName);

            _topicClient = TopicClient.CreateFromConnectionString(connectionString, topicName);
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            // do nothing
        }

        public void Dispatch(IEnumerable<DomainEvent> events)
        {
            var domainEvents = events.ToList();

            if (!domainEvents.Any()) return;

            var notification = new DispatchNotification
            {
                DomainEvents = _serializer.Serialize(domainEvents)
            };

            _topicClient.Send(new BrokeredMessage(notification));
        }
    }
}