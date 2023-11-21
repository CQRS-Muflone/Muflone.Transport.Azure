using Microsoft.Extensions.Hosting;
using Muflone.Transport.Azure.Abstracts;

namespace Muflone.Transport.Azure;

public class AzureBrokerStarter : IHostedService
{
	private readonly IEnumerable<IConsumer> _consumers;

	public AzureBrokerStarter(IEnumerable<IConsumer> consumers)
	{
		_consumers = consumers;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		foreach (var consumer in _consumers)
		{
			await consumer.StartAsync(cancellationToken);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}