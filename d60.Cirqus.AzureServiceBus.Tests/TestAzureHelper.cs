using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Tests
{
    public static class TestAzureHelper
    {
        static readonly List<string> TopicsToCleanUp = new List<string>();

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

            TopicsToCleanUp.ForEach(topicName =>
            {
                try
                {
                    Console.WriteLine("Deleting topic: {0}", topicName);
                    manager.DeleteTopic(topicName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            });

            TopicsToCleanUp.Clear();
        }

        public static string GetPath(string path)
        {
            return PossiblyWithTcAgentNumber(path);
        }

        public static string GetTopicName(string topicName)
        {
            var returnedTopicName = PossiblyWithTcAgentNumber(topicName);
            
            TopicsToCleanUp.Add(returnedTopicName);
            
            return returnedTopicName;
        }

        public static string GetSubscriptionName(string subscriptionName)
        {
            return PossiblyWithTcAgentNumber(subscriptionName);
        }

        static string PossiblyWithTcAgentNumber(string path)
        {
            var teamCityAgentNumber = Environment.GetEnvironmentVariable("tcagent");
            int number;

            if (string.IsNullOrWhiteSpace(teamCityAgentNumber) || !int.TryParse(teamCityAgentNumber, out number))
                return path;

            return string.Format("{0}agent{1}", path, number);
        }
    }
}