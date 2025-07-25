using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Basses.SimpleEventStore.Projections;

internal class ProjectionManagerBackgroundService(ProjectionManager projectionManager, ILogger<ProjectionManagerBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return projectionManager.RunAsync(logger, stoppingToken);
    }
}
