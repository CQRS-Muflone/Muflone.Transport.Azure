using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;
using System.Globalization;

namespace Muflone.Transport.Azure.Consumers;

/// <summary>
/// TODO: Unify Consumer: now we have different Consumers for Command and DomainEvent
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class CommandConsumerBase<T> : ICommandConsumer<T>, IAsyncDisposable where T : class, ICommand
{
	public string TopicName { get; }

	private readonly ServiceBusProcessor _processor;
	private readonly Persistence.ISerializer _messageSerializer;
	private readonly ILogger _logger;

	protected abstract ICommandHandlerAsync<T> HandlerAsync { get; }

	protected CommandConsumerBase(AzureServiceBusConfiguration azureServiceBusConfiguration,
		ILoggerFactory loggerFactory,
		Persistence.ISerializer? messageSerializer = null)
	{
		TopicName = typeof(T).Name;

		_logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));

		var serviceBusClient = new ServiceBusClient(azureServiceBusConfiguration.ConnectionString);
		_messageSerializer = messageSerializer ?? new Persistence.Serializer();

		// Create Queue on Azure ServiceBus if missing
		ServiceBusAdministrator.CreateQueueIfNotExistAsync(new AzureQueueReferences(TopicName, "",
			azureServiceBusConfiguration.ConnectionString)).GetAwaiter().GetResult();

		_processor = serviceBusClient.CreateProcessor(
			topicName: TopicName.ToLower(CultureInfo.InvariantCulture),
			subscriptionName: "", new ServiceBusProcessorOptions
			{
				AutoCompleteMessages = false,
				MaxConcurrentCalls = azureServiceBusConfiguration.MaxConcurrentCalls
			});
		_processor.ProcessMessageAsync += AzureMessageHandler;
		_processor.ProcessErrorAsync += ProcessErrorAsync;
	}

	public Task ConsumeAsync(T message, CancellationToken cancellationToken = default)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		return ConsumeAsyncCore(message, cancellationToken);
	}

	private Task ConsumeAsyncCore<T>(T message, CancellationToken cancellationToken)
	{
		try
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			HandlerAsync.HandleAsync((dynamic)message, cancellationToken);

			return Task.CompletedTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex,
				"An error occurred processing command {Name}. StackTrace: {ExStackTrace} - Source: {ExSource} - Message: {ExMessage}",
				typeof(T).Name, ex.StackTrace, ex.Source, ex.Message);
			throw;
		}
	}

	public async Task StartAsync(CancellationToken cancellationToken = default) =>
		await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

	public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;


	private async Task AzureMessageHandler(ProcessMessageEventArgs args)
	{
		try
		{
			_logger.LogInformation("Received message \'{MessageMessageId}\'. Processing...", args.Message.MessageId);

			var message = await _messageSerializer.DeserializeAsync<T>(args.Message.Body.ToString());

			await ConsumeAsync(message, args.CancellationToken);

			await args.CompleteMessageAsync(args.Message).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "an error has occurred while processing message \'{MessageMessageId}\': {ExMessage}",
				args.Message.MessageId, ex.Message);
			if (args.Message.DeliveryCount > 3)
				await args.DeadLetterMessageAsync(args.Message).ConfigureAwait(false);
			else
				await args.AbandonMessageAsync(args.Message).ConfigureAwait(false);
		}
	}

	private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
	{
		_logger.LogError(arg.Exception,
			"An exception has occurred while processing message \'{ArgFullyQualifiedNamespace}\'",
			arg.FullyQualifiedNamespace);
		return Task.CompletedTask;
	}

	#region Dispose

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;

	#endregion
}