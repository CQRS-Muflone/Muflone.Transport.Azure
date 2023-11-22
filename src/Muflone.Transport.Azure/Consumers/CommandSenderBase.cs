using Azure.Messaging.ServiceBus;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;
using System.Globalization;

namespace Muflone.Transport.Azure.Consumers;

public abstract class CommandSenderBase<T> : ICommandSender<T>, IAsyncDisposable where T : class, ICommand
{
	public string TopicName { get; }

	private readonly ServiceBusProcessor _processor;

	public CommandSenderBase(AzureServiceBusConfiguration azureServiceBusConfiguration)
	{
		TopicName = typeof(T).Name;

		var serviceBusClient = new ServiceBusClient(azureServiceBusConfiguration.ConnectionString);

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
	}

	public async Task StartAsync(CancellationToken cancellationToken = default) =>
		await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);

	public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	#region Dispose
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	#endregion
}