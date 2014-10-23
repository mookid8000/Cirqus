using System;
using System.IO;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Tests
{
    public class TestAzureHelper
    {
        public const string TopicName = "cirqus";
        public const string SubscriptionName = "test";
        
        public static readonly string ConnectionString = GetConnectionString();

        public static string KeyName
        {
            get { return ConnectionString.Split(';')[1].Split('=')[1]; }
        }

        public static string SharedAccessKey
        {
            get { return ConnectionString.Split(';')[2].Split('=')[1] + "="; }
        }

        static string GetConnectionString()
        {
            return File.ReadAllText(@"..\..\..\azure_service_bus_connection_string.txt");
        }

        public static void CleanUp()
        {
            var manager = NamespaceManager.CreateFromConnectionString(ConnectionString);
            try
            {
                Console.WriteLine("Deleting subscription: {0}/{1}", TopicName, SubscriptionName);
                manager.DeleteSubscription(TopicName, SubscriptionName);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

            try
            {
                Console.WriteLine("Deleting topic: {0}", TopicName);
                manager.DeleteTopic(TopicName);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}