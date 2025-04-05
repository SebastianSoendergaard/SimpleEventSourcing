using System.Text;
using Basses.SimpleEventStore.EventStore;
using Microsoft.Extensions.DependencyInjection;

namespace Basses.SimpleEventStore.EventSubscriber;

public abstract class EventSubscriptionManager
{
    private readonly IEventStore _eventStore;
    private readonly IEventSubscriberStateStore _subscriberStateStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<Type> _synchronousSubscribers = [];
    private readonly List<Type> _asynchronousSubscribers = [];

    public EventSubscriptionManager(IEventStore eventStore, IEventSubscriberStateStore subscriberStateStore, IServiceProvider serviceProvider)
    {
        _eventStore = eventStore;
        _subscriberStateStore = subscriberStateStore;
        _serviceProvider = serviceProvider;
        _eventStore.RegisterForEventsAppendedNotifications(NotifySynchronousSubscribers);
    }

    protected void RegisterSynchronousSubscriber<TSubscriber>() where TSubscriber : IEventSubscriber
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriber = scope.ServiceProvider.GetRequiredService<TSubscriber>();
        _subscriberStateStore.UpsertSubscriber(subscriber);
        _synchronousSubscribers.Add(subscriber.GetType());
    }

    protected void RegisterAsynchronousSubscriber<TSubscriber>() where TSubscriber : IEventSubscriber
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriber = scope.ServiceProvider.GetRequiredService<TSubscriber>();
        _subscriberStateStore.UpsertSubscriber(subscriber);
        _synchronousSubscribers.Add(subscriber.GetType());
    }

    private Task NotifySynchronousSubscribers()
    {
        return NotifySubscribers(_synchronousSubscribers);
    }

    private Task NotifyAsynchronousSubscribers()
    {
        return NotifySubscribers(_asynchronousSubscribers);

        // TODO: continue updates if not done yet
    }

    private async Task NotifySubscribers(IEnumerable<Type> subscriberTypes)
    {
        using var scope = _serviceProvider.CreateScope();

        var headSequenceNumber = await _eventStore.GetHeadSequenceNumber();
        var eventCache = new Dictionary<long, IEnumerable<EventEntry>>();

        foreach (var subscriberType in subscriberTypes)
        {
            var subscriber = (IEventSubscriber)scope.ServiceProvider.GetRequiredService(subscriberType);

            var currentState = await _subscriberStateStore.GetProcessingState(subscriber);

            try
            {
                var events = await LoadEvents(subscriber, currentState, headSequenceNumber, eventCache);
                if (events.Any())
                {
                    await ApplyEvents(subscriber, currentState, events);

                    var newState = new EventSubscriberProcessingState(DateTimeOffset.UtcNow, events.Last().SequenceNumber);
                    await _subscriberStateStore.SaveProcessingState(subscriber, newState);
                }
            }
            catch (Exception ex)
            {
                var error = new EventSubscriberProcessingError(PrepareErrorMessage(ex), ex.StackTrace ?? "", currentState.ProcessingError?.ProcessingAttempts + 1 ?? 1, DateTimeOffset.UtcNow);
                var newState = new EventSubscriberProcessingState(currentState.LatestSuccessfulProcessingTime, currentState.ConfirmedSequenceNumber, error);
                await _subscriberStateStore.SaveProcessingState(subscriber, newState);
            }
        }
    }

    private string PrepareErrorMessage(Exception exception)
    {
        var sb = new StringBuilder();
        var ex = exception;
        while (ex != null)
        {
            sb.Append(ex.Message).Append(" | ");
            ex = ex.InnerException;
        }
        return sb.ToString();
    }

    private async Task<IEnumerable<EventEntry>> LoadEvents(IEventSubscriber subscriber, EventSubscriberProcessingState currentState, long headSequenceNumber, Dictionary<long, IEnumerable<EventEntry>> eventCache)
    {
        var sequenceNumber = await subscriber.GetSequenceNumber(currentState);

        if (eventCache.ContainsKey(sequenceNumber))
        {
            return eventCache[sequenceNumber];
        }

        if (sequenceNumber < headSequenceNumber)
        {
            const int max = 50; // We need a limit to ensure that one projector does not block them all
            var events = await _eventStore.LoadEvents(sequenceNumber + 1, max);
            eventCache.Add(sequenceNumber, events);
            return events;
        }

        return [];
    }

    private async Task ApplyEvents(IEventSubscriber subscriber, EventSubscriberProcessingState currentState, IEnumerable<EventEntry> events)
    {
        if (currentState.ProcessingError != null)
        {
            // Process one event at the time until we reach the failing one
            foreach (var @event in events)
            {
                await subscriber.Update([@event]);
            }
        }
        else
        {
            // We expect every thing to be ok, so lets try to process the entire batch
            await subscriber.Update(events);
        }
    }

    protected IEnumerable<Type> GetSubscriberTypes()
    {
        return _synchronousSubscribers.Concat(_asynchronousSubscribers);
    }

    protected Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber projector)
    {
        return _subscriberStateStore.GetProcessingState(projector);
    }
}
