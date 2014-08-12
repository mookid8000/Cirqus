using System;
using System.Threading;
using d60.Circus.Events;
using Microsoft.ServiceBus.Messaging;

namespace d60.Circus.AzureServiceBus
{
    public class AzureServiceBusEventDispatcherReceiver : IDisposable
    {
        readonly Serializer _serializer = new Serializer();
        readonly IEventDispatcher _innerEventDispatcher;
        readonly IEventStore _eventStore;
        readonly SubscriptionClient _subscriptionClient;
        readonly Thread _workerThread;

        bool _keepWorking = true;

        public AzureServiceBusEventDispatcherReceiver(string connectionString, IEventDispatcher innerEventDispatcher, IEventStore eventStore, string topicName, string subscriptionName)
        {
            _innerEventDispatcher = innerEventDispatcher;
            _eventStore = eventStore;

            AzureHelpers.EnsureTopicExists(connectionString, topicName);
            AzureHelpers.EnsureSubscriptionExists(connectionString, topicName, subscriptionName);

            _subscriptionClient = SubscriptionClient.CreateFromConnectionString(connectionString, topicName, subscriptionName);

            _workerThread = new Thread(DoWork);
        }

        public void Initialize(bool purgeExistingViews = false)
        {
            _innerEventDispatcher.Initialize(_eventStore, purgeExistingViews: purgeExistingViews);

            _workerThread.Start();
        }

        void DoWork()
        {
            Console.WriteLine("Starting worker thread...");
            while (_keepWorking)
            {
                try
                {
                    var message = _subscriptionClient.Receive(TimeSpan.FromSeconds(1));
                    if (message == null) continue;

                    var notification = message.GetBody<DispatchNotification>();

                    _innerEventDispatcher.Dispatch(_eventStore, _serializer.Deserialize(notification.DomainEvents));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Thread.Sleep(2000);
                }
            }
            Console.WriteLine("Worker thread stopped");
        }

        public void Dispose()
        {
            _keepWorking = false;
            _workerThread.Join();
        }
    }
}