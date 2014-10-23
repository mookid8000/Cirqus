using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using d60.Cirqus.Events;
using d60.Cirqus.Logging;
using d60.Cirqus.Views;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    /// <summary>
    /// Event dispatcher implementation that can relay events to any number of <see cref="AzureServiceBusRelayEventStoreFacade"/>s
    /// </summary>
    public class AzureServiceBusRelayEventDispatcher : IEventDispatcher, IDisposable
    {
        static Logger _logger;

        static AzureServiceBusRelayEventDispatcher()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly ServiceHost _serviceHost;
        bool _disposed;

        public AzureServiceBusRelayEventDispatcher(IEventStore eventStore, string serviceNamespace, string path, string keyName, string sharedAccessKey)
        {
            var uri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, path);

            _logger.Info("Initializing service bus relay host for {0}", uri);

            _serviceHost = new ServiceHost(new HostService(eventStore));

            var binding = new NetTcpRelayBinding();
            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, sharedAccessKey);

            var endpoint = _serviceHost.AddServiceEndpoint(typeof(IHostService), binding, uri);
            var endpointBehavior = new TransportClientEndpointBehavior(tokenProvider);
            endpoint.Behaviors.Add(endpointBehavior);

        }

        ~AzureServiceBusRelayEventDispatcher()
        {
            Dispose(false);
        }

        public void Initialize(IEventStore eventStore, bool purgeExistingViews = false)
        {
            _logger.Info("Opening connection");
            _serviceHost.Open();
        }

        public void Dispatch(IEventStore eventStore, IEnumerable<DomainEvent> events)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposed) return;

                try
                {
                    _logger.Info("Closing host");
                    _serviceHost.Close(TimeSpan.FromSeconds(10));
                }
                catch(Exception exception)
                {
                    _logger.Warn(exception, "An error occurred while closing the host");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }

    [ServiceContract(Namespace = "urn:cirqus")]
    public interface IHostService
    {
        [OperationContract]
        TransportMessage Load(Guid aggregateRootId, long firstSeq);

        [OperationContract]
        TransportMessage Stream(long globalSequenceNumber);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class HostService : IHostService
    {
        readonly Serializer _serializer = new Serializer();
        readonly IEventStore _eventStore;

        public HostService(IEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public TransportMessage Load(Guid aggregateRootId, long firstSeq)
        {
            var domainEvents = _eventStore
                .Load(aggregateRootId, firstSeq)
                .ToList();

            return new TransportMessage
            {
                Events = _serializer.Serialize(domainEvents)
            };
        }

        public TransportMessage Stream(long globalSequenceNumber)
        {
            var domainEvents = _eventStore
                .Stream(globalSequenceNumber)
                .Take(1000)
                .ToList();

            return new TransportMessage
            {
                Events = _serializer.Serialize(domainEvents)
            };
        }
    }
}