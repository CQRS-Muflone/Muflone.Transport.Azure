using Azure.Messaging.ServiceBus;
using Muflone.Messages.Commands;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;
using System.Globalization;

namespace Muflone.Transport.Azure.Consumers;

public abstract class CommandSenderBase<T> : ICommandSender<T>, IAsyncDisposable where T : Command
{
	private string TopicName { get; }

	private readonly ServiceBusProcessor _processor;

	protected CommandSenderBase(AzureServiceBusConfiguration azureServiceBusConfiguration)
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
		_processor.ProcessMessageAsync += AzureMessageHandler;
		_processor.ProcessErrorAsync += ProcessErrorAsync;
	}

	private Task AzureMessageHandler(ProcessMessageEventArgs args)
	{
		return Task.CompletedTask;
	}

	private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
	{
		return Task.CompletedTask;
	}

	public async Task StartAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		await _processor.StartProcessingAsync(cancellationToken).ConfigureAwait(false);
	}
		
	public Task StopAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		return Task.CompletedTask;
	}

	#region Dispose
	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	#endregion
}