using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages;
using Muflone.Messages.Commands;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.Azure.Factories;

namespace Muflone.Transport.Azure;

public class ServiceBus : IServiceBus, IEventBus
{
	private readonly IServiceBusSenderFactory _senderFactory;
	private readonly ILogger<ServiceBus> _logger;
	private readonly ISerializer _messageSerializer;

	public ServiceBus(IServiceBusSenderFactory senderFactory,
		ILogger<ServiceBus> logger)
	{
		_senderFactory = senderFactory ?? throw new ArgumentNullException(nameof(senderFactory));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_messageSerializer = new Serializer();
	}

	public Task SendAsync<T>(T command, CancellationToken cancellationToken = default) where T : class, ICommand
	{
		if (command == null)
			throw new ArgumentNullException(nameof(command));

		return SendAsyncCore(command, cancellationToken);
	}

	private async Task SendAsyncCore<T>(T command, CancellationToken cancellationToken = default) where T : class, ICommand
	{
		var sender = _senderFactory.Create(command);
		_logger.LogInformation(
			"Send command \'{CommandMessageId}\' to {SenderFullyQualifiedNamespace}/{SenderEntityPath}",
			command.MessageId, sender.FullyQualifiedNamespace, sender.EntityPath);

		var serializedMessage = await _messageSerializer.SerializeAsync(command, cancellationToken);

		var correlationPair = command.UserProperties.FirstOrDefault(u =>
			u.Key.Equals(HeadersNames.CorrelationId, StringComparison.InvariantCultureIgnoreCase));
		var correlationId = string.Empty;
		if (correlationPair.Value != null)
			correlationId = correlationPair.Value.ToString();

		var busMessage = new ServiceBusMessage(serializedMessage)
		{
			CorrelationId = correlationId,
			MessageId = command.MessageId.ToString(),
			ApplicationProperties = { { "CommandName", command.GetType().FullName } }
		};

		await sender.SendMessageAsync(busMessage, cancellationToken).ConfigureAwait(false);
	}
	
	public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class, IEvent
	{
		if (@event == null)
			throw new ArgumentNullException(nameof(@event));

		return PublishAsyncCore(@event, cancellationToken);
	}

	public async Task PublishAsyncCore<T>(T @event, CancellationToken cancellationToken = default) where T : class, IEvent
	{
		var sender = _senderFactory.Create(@event);
		_logger.LogInformation(
			"Publishing event \'{EventMessageId}\' to {SenderFullyQualifiedNamespace}/{SenderEntityPath}",
			@event.MessageId, sender.FullyQualifiedNamespace, sender.EntityPath);

		var serializedMessage = await _messageSerializer.SerializeAsync(@event, cancellationToken);

		var correlationId = string.Empty;
		if (@event.UserProperties != null)
		{
			var correlationPair = @event.UserProperties.FirstOrDefault(u => u.Key.Equals(HeadersNames.CorrelationId));
			if (correlationPair.Value != null)
				correlationId = correlationPair.Value.ToString();
		}

		var busMessage = new ServiceBusMessage(serializedMessage)
		{
			CorrelationId = correlationId,
			MessageId = @event.MessageId.ToString(),
			ApplicationProperties = { { "EventName", @event.GetType().FullName } }
		};

		await sender.SendMessageAsync(busMessage, cancellationToken).ConfigureAwait(false);
	}
}