using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using d60.Cirqus.AzureServiceBus.Config;
using d60.Cirqus.Events;
using d60.Cirqus.Extensions;
using d60.Cirqus.Logging;
using d60.Cirqus.Numbers;
using d60.Cirqus.Serialization;
using Microsoft.ServiceBus;

namespace d60.Cirqus.AzureServiceBus.Relay
{
    /// <summary>
    /// Event store proxy that can be used to stream events from a <see cref="AzureServiceBusRelayEventDispatcher"/>, possibly installed
    /// as an event dispatcher in a command processor by using the <see cref="AzureServiceBusConfigurationExtensions.UseAzureServiceBusRelayEventDispatcher"/>
    /// configuration method. The event store proxy should most likely be used by a <see cref="EventReplicator"/> to move events off-site, and
    /// then any views and stuff can do their work off of the off-site copy
    /// </summary>
    public class AzureServiceBusRelayEventStoreProxy : IEventStore, IDisposable
    {
        static readonly Encoding DefaultEncoding = Encoding.UTF8;

        static Logger _logger;

        static AzureServiceBusRelayEventStoreProxy()
        {
            CirqusLoggerFactory.Changed += f => _logger = f.GetCurrentClassLogger();
        }

        readonly MetadataSerializer _metadataSerializer = new MetadataSerializer();
        readonly ChannelFactory<IHostService> _channelFactory;

        IHostService _currentClientChannel;
        bool _disposed;

        public AzureServiceBusRelayEventStoreProxy(string serviceNamespace, string servicePath, string keyName, string sharedAccessKey, NetTcpRelayBinding netTcpRelayBinding = null)
        {
            var uri = ServiceBusEnvironment.CreateServiceUri("sb", serviceNamespace, servicePath);

            _logger.Info("Initializing event store facade for {0}", uri);

            var binding = netTcpRelayBinding ?? BindingHelper.CreateDefaultRelayBinding();
            _channelFactory = new ChannelFactory<IHostService>(binding, new EndpointAddress(uri));

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, sharedAccessKey);
            var endpointBehavior = new TransportClientEndpointBehavior(tokenProvider);
            _channelFactory.Endpoint.Behaviors.Add(endpointBehavior);
        }

        ~AzureServiceBusRelayEventStoreProxy()
        {
            Dispose(false);
        }

        public void Save(Guid batchId, IEnumerable<DomainEvent> batch)
        {
            throw new InvalidOperationException();
        }

        public long GetNextGlobalSequenceNumber()
        {
            throw new InvalidOperationException();
        }

        public void Save(Guid batchId, IEnumerable<Event> events)
        {
        }

        public IEnumerable<Event> LoadNew(Guid aggregateRootId, long firstSeq = 0)
        {
            return Enumerable.Empty<Event>();
        }

        public IEnumerable<Event> StreamNew(long globalSequenceNumber = 0)
        {
            return Enumerable.Empty<Event>();
        }

        public IEnumerable<Event> Load(Guid aggregateRootId, long firstSeq = 0)
        {
            var transportMessage = InnerLoad(aggregateRootId, firstSeq);
            
            var domainEvents = transportMessage.Events
                .Select(e => Event.FromMetadata(DeserializeMetadata(e), e.Data));

            return domainEvents;
        }

        public IEnumerable<Event> Stream(long globalSequenceNumber = 0)
        {
            var currentPosition = globalSequenceNumber;

            while (true)
            {
                var transportMessage = InnerStream(currentPosition);

                var domainEvents = transportMessage.Events
                    .Select(e => Event.FromMetadata(DeserializeMetadata(e), e.Data))
                    .ToList();

                if (!domainEvents.Any())
                {
                    yield break;
                }

                foreach (var e in domainEvents)
                {
                    yield return e;

                    currentPosition = e.GetGlobalSequenceNumber() + 1;
                }
            }
        }

        TransportMessage InnerStream(long currentPosition)
        {
            try
            {
                return GetClient().Stream(currentPosition);

            }
            catch
            {
                DisposeCurrentClient();
                throw;
            }
        }

        TransportMessage InnerLoad(Guid aggregateRootId, long firstSeq)
        {
            try
            {
                return GetClient().Load(aggregateRootId, firstSeq);
            }
            catch
            {
                DisposeCurrentClient();
                throw;
            }
        }

        void DisposeCurrentClient()
        {
            if (_currentClientChannel == null) return;

            _currentClientChannel = null;
        }

        Metadata DeserializeMetadata(TransportEvent e)
        {
            return _metadataSerializer.Deserialize(DefaultEncoding.GetString(e.Meta));
        }

        IHostService GetClient()
        {
            if (_currentClientChannel != null)
            {
                return _currentClientChannel;
            }

            _currentClientChannel = _channelFactory.CreateChannel();

            return _currentClientChannel;
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
                    _logger.Info("Disposing channel factory");
                    _channelFactory.Close(TimeSpan.FromSeconds(10));
                }
                catch (Exception exception)
                {
                    _logger.Warn(exception, "An error occurred while closing the channel factory");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}