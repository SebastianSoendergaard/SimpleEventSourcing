using Basses.SimpleEventStore.Reactions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Basses.SimpleEventStore.Projections;

internal class ReactionManagerBackgroundService(ReactionManager reactionManager, ILogger<ProjectionManagerBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return reactionManager.RunAsync(logger, stoppingToken);
    }
}
