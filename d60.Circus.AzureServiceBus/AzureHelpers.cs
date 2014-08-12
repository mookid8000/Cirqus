using Microsoft.ServiceBus;

namespace d60.Circus.AzureServiceBus
{
    class AzureHelpers
    {
        public static void EnsureTopicExists(string connectionString, string topicName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (namespaceManager.TopicExists(topicName)) return;

            try
            {
                namespaceManager.CreateTopic(topicName);
            }
            catch
            {
                if (namespaceManager.TopicExists(topicName)) return;

                throw;
            }
        }

        public static void EnsureSubscriptionExists(string connectionString, string topicName, string subscriptionName)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (namespaceManager.SubscriptionExists(topicName, subscriptionName)) return;

            try
            {
                namespaceManager.CreateSubscription(topicName, subscriptionName);
            }
            catch
            {
                if (namespaceManager.SubscriptionExists(topicName, subscriptionName)) return;

                throw;
            }
        }
    }
}