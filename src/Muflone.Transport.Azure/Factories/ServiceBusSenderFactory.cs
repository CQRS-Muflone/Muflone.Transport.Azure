using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Muflone.Messages;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public class ServiceBusSenderFactory : IAsyncDisposable, IServiceBusSenderFactory
{
	private readonly ServiceBusClient _serviceBusClient;
	private readonly ConcurrentDictionary<AzureQueueReferences, ServiceBusSender> _senders = new();

	private readonly AzureServiceBusConfiguration _configuration;

	public ServiceBusSenderFactory(ServiceBusClient serviceBusClient,
		AzureServiceBusConfiguration configuration)
	{
		_serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
		_configuration = configuration;
	}

	public ServiceBusSender Create<T>(T message) where T : IMessage
	{
		var configuration = new AzureServiceBusConfiguration(_configuration.ConnectionString, message.GetType().Name, _configuration.ClientId );

		var references = new AzureQueueReferences(message.GetType().Name, $"{configuration!.ClientId}-subscription",
			configuration!.ConnectionString);
		var sender = _senders.GetOrAdd(references, _ => _serviceBusClient.CreateSender(references.TopicName));

		if (sender is null || sender.IsClosed)
			sender = _senders[references] = _serviceBusClient.CreateSender(references.TopicName);

		return sender;
	}

	public async ValueTask DisposeAsync()
	{
		foreach (var sender in _senders.Values)
			await sender.DisposeAsync();

		_senders.Clear();
	}
}