using Basses.SimpleEventStore.Reactions;
using Microsoft.Extensions.Hosting;

namespace Basses.SimpleEventStore.Projections;

internal class ReactionManagerBackgroundService(ReactionManager reactionManager) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return reactionManager.RunAsync(stoppingToken);
    }
}
