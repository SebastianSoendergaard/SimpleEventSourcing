using EventSourcing.EventStore;
using EventSourcing.Projections;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]/")]
public class SupportController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly ProjectionManager _projectionManager;

    public SupportController(IEventStore eventStore, ProjectionManager projectionManager)
    {
        _eventStore = eventStore;
        _projectionManager = projectionManager;
    }

    [HttpGet("GetStreamEvents/{streamId:guid}")]
    public async Task<object> GetStreamEvents(Guid streamId)
    {
        var events = await _eventStore.LoadEvents(streamId);
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
        var projectors = _projectionManager.GetProjectors();

        var projectorStates = new List<object>();

        foreach (var projector in projectors)
        {
            var sequenceNumber = await projector.LoadSequenceNumber();
            var processingState = await _projectionManager.GetProcessingState(projector.Id);

            projectorStates.Add(new
            {
                projector.Id,
                projector.Name,
                sequenceNumber,
                processingState
            });
        }

        return projectorStates;
    }

    [HttpGet("GetProjectorState/{projectorId:guid}")]
    public async Task<object> GetProjectorState(Guid projectorId)
    {
        var state = await _projectionManager.GetProcessingState(projectorId);
        return state;
    }
}
