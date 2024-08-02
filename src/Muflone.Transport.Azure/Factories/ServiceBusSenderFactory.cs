using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Muflone.Messages;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

public class ServiceBusSenderFactory(
	ServiceBusClient serviceBusClient,
	AzureServiceBusConfiguration configuration)
	: IAsyncDisposable, IServiceBusSenderFactory
{
	private readonly ServiceBusClient _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
	private readonly ConcurrentDictionary<AzureQueueReferences, ServiceBusSender> _senders = new();

	public ServiceBusSender Create<T>(T message) where T : IMessage
	{
		var configuration1 = new AzureServiceBusConfiguration(configuration.ConnectionString, message.GetType().Name, configuration.ClientId );

		var references = new AzureQueueReferences(message.GetType().Name, $"{configuration1!.ClientId}-subscription",
			configuration1!.ConnectionString);
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