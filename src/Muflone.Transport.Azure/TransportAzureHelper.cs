using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Muflone.Persistence;
using Muflone.Transport.Azure.Abstracts;
using Muflone.Transport.Azure.Factories;
using Muflone.Transport.Azure.Models;

namespace Muflone.Transport.Azure;

public static class TransportAzureHelper
{
	public static IServiceCollection AddMufloneTransportAzure(this IServiceCollection services,
		AzureServiceBusConfiguration azureServiceBusConfiguration)
	{
		var serviceProvider = services.BuildServiceProvider();
		var serviceBus = serviceProvider.GetService<IServiceBus>();
		if (serviceBus != null)
			return services;
		
		services.AddSingleton(Enumerable.Empty<IConsumer>());
		services.AddSingleton(new ServiceBusClient(azureServiceBusConfiguration.ConnectionString));
		services.AddSingleton(azureServiceBusConfiguration);
		services.AddSingleton<IServiceBusSenderFactory, ServiceBusSenderFactory>();
		services.AddSingleton<IServiceBus, ServiceBus>();
		services.AddSingleton<IEventBus, ServiceBus>();
		
		services.AddHostedService<AzureBrokerStarter>();

		return services;
	}

	public static IServiceCollection AddMufloneAzureConsumers(this IServiceCollection services,
		IEnumerable<IConsumer> messageConsumers)
	{
		services.AddSingleton(messageConsumers);
		
		return services;
	}
}