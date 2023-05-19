﻿using Microsoft.Extensions.Hosting;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure;

public class AzureBrokerStarter : IHostedService
{
	private readonly IEnumerable<IConsumer> _consumers;
	private readonly AzureServiceBusConfiguration _azureServiceBusConfiguration;

	public AzureBrokerStarter(IEnumerable<IConsumer> consumers,
		AzureServiceBusConfiguration azureServiceBusConfiguration)
	{
		_consumers = consumers;
		_azureServiceBusConfiguration = azureServiceBusConfiguration;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		var configurations = Enumerable.Empty<AzureServiceBusConfiguration>();
		foreach (var consumer in _consumers)
		{
			await consumer.StartAsync(cancellationToken);
			configurations = configurations.Concat(new List<AzureServiceBusConfiguration>
			{
				new(_azureServiceBusConfiguration.ConnectionString, consumer.TopicName,
					_azureServiceBusConfiguration.ClientId)
			});
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}