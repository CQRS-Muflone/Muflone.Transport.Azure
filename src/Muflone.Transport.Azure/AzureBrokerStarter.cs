using Microsoft.Extensions.Hosting;
using Muflone.Transport.Azure.Abstracts;

namespace Muflone.Transport.Azure;

public class AzureBrokerStarter(IEnumerable<IConsumer> consumers) : IHostedService
{
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		foreach (var consumer in consumers)
		{
			await consumer.StartAsync(cancellationToken);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}