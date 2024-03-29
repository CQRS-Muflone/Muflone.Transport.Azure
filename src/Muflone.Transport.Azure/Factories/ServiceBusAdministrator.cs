﻿using Azure.Messaging.ServiceBus.Administration;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure.Factories;

//TODO: Must handle exceptions in case of connection errors! We can implement a new property ConnectionString and use it to check write rights
public static class ServiceBusAdministrator
{
	public static async Task CreateTopicIfNotExistAsync(AzureServiceBusConfiguration azureServiceBusConfiguration)
	{
		var adminClient = new ServiceBusAdministrationClient(azureServiceBusConfiguration.ConnectionString);
		var topicExists = await adminClient.TopicExistsAsync(azureServiceBusConfiguration.TopicName);

		if (!topicExists)
		{
			var options = new CreateTopicOptions(azureServiceBusConfiguration.TopicName)
			{
				MaxSizeInMegabytes = 1024
			};
			await adminClient.CreateTopicAsync(options);
		}

		var subscriptionExists =
			await adminClient.SubscriptionExistsAsync(azureServiceBusConfiguration.TopicName,
				$"{azureServiceBusConfiguration.ClientId}-subscription");
		if (!subscriptionExists)
		{
			var options = new CreateSubscriptionOptions(azureServiceBusConfiguration.TopicName,
				$"{azureServiceBusConfiguration.ClientId}-subscription")
			{
				DefaultMessageTimeToLive = new TimeSpan(14, 0, 0, 0),
				DeadLetteringOnMessageExpiration = true,
				EnableDeadLetteringOnFilterEvaluationExceptions = true
			};
			await adminClient.CreateSubscriptionAsync(options);
		}
	}

	public static async Task CreateQueueIfNotExistAsync(AzureQueueReferences azureQueueReferences)
	{
		var adminClient = new ServiceBusAdministrationClient(azureQueueReferences.ConnectionString);
		var queueExists = await adminClient.QueueExistsAsync(azureQueueReferences.TopicName);

		if (!queueExists)
		{
			var options = new CreateQueueOptions(azureQueueReferences.TopicName)
			{
				MaxDeliveryCount = 10,
				DeadLetteringOnMessageExpiration = true
			};
			await adminClient.CreateQueueAsync(options);
		}
	}
}