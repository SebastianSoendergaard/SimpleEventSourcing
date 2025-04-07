using Microsoft.Extensions.Hosting;

namespace Basses.SimpleEventStore.Projections;

internal class ProjectionManagerBackgroundService(ProjectionManager projectionManager) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return projectionManager.RunAsync(stoppingToken);
    }
}
