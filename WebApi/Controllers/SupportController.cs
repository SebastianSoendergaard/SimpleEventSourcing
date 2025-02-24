using Basses.SimpleEventStore.EventStore;
using Basses.SimpleEventStore.Projections;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]/")]
public class SupportController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ProjectionManager _projectionManager;
    private readonly IServiceProvider _serviceProvider;

    public SupportController(IEventStore eventStore, ProjectionManager projectionManager, IServiceProvider serviceProvider)
    {
        _eventStore = eventStore;
        _projectionManager = projectionManager;
        _serviceProvider = serviceProvider;
    }

    [HttpGet("GetStreamEvents/{streamId:guid}")]
    public async Task<object> GetStreamEvents(Guid streamId)
    {
        var events = await _eventStore.LoadEvents(streamId.ToString());
        return events;
    }

    [HttpGet("GetNewestEvents/{max:int}")]
    public async Task<object> GetNewestEvents(int max)
    {
        var head = await _eventStore.GetHeadSequenceNumber();
        var startSequenceNumber = head - max;

        var events = await _eventStore.LoadEvents(startSequenceNumber, max);
        return events;
    }

    [HttpGet("GetProjectors")]
    public async Task<object> GetProjectors()
    {
        var projectorTypes = _projectionManager.GetProjectorTypes();

        var projectorStates = new List<object>();

        foreach (var projectorType in projectorTypes)
        {
            var projector = (IProjector)_serviceProvider.GetRequiredService(projectorType);

            var sequenceNumber = await projector.GetSequenceNumber();
            var processingState = await _projectionManager.GetProcessingState(projector);

            projectorStates.Add(new
            {
                projector.Name,
                sequenceNumber,
                processingState
            });
        }

        return projectorStates;
    }
}
