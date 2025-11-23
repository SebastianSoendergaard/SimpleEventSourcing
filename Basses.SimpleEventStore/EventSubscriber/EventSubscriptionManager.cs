using System.Text;
using Basses.SimpleEventStore.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Basses.SimpleEventStore.EventSubscriber;

public abstract class EventSubscriptionManager
{
    private readonly IEventStore _eventStore;
    private readonly IEventSubscriberStateStore _subscriberStateStore;
    private readonly SubscriberRegister _subscriberRegister;
    private readonly IServiceProvider _serviceProvider;

    public EventSubscriptionManager(IEventStore eventStore, IEventSubscriberStateStore subscriberStateStore, SubscriberRegister subscriberRegister, IServiceProvider serviceProvider)
    {
        _eventStore = eventStore;
        _subscriberStateStore = subscriberStateStore;
        _subscriberRegister = subscriberRegister;
        _serviceProvider = serviceProvider;

        _eventStore.RegisterForEventsAppendedNotifications(() => NotifySynchronousSubscribers(CancellationToken.None));
        RegisterSubscribers();
    }

    private void RegisterSubscribers()
    {
        using var scope = _serviceProvider.CreateScope();
        foreach (var subscriberType in _subscriberRegister.AllSubscribers)
        {
            var subscriber = (IEventSubscriber)scope.ServiceProvider.GetRequiredService(subscriberType);
            _subscriberStateStore.UpsertSubscriber(subscriber).GetAwaiter().GetResult();
        }
    }

    private Task NotifySynchronousSubscribers(CancellationToken cancellationToken)
    {
        return NotifySubscribers(_subscriberRegister.SynchronousSubscribers, cancellationToken);
    }

    protected async Task NotifyAsynchronousSubscribers(ILogger logger, CancellationToken cancellationToken)
    {
        if (!_subscriberRegister.AsynchronousSubscribers.Any())
        {
            logger.LogInformation(GetType().Name + " no asynchronous subscribers registered");
            return;
        }

        logger.LogInformation(GetType().Name + " starting notification of asynchronous subscribers");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await NotifySubscribers(_subscriberRegister.AsynchronousSubscribers, cancellationToken);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception while notifying asynchronous subscribers");
            }
        }

        logger.LogInformation(GetType().Name + " stopping notification of asynchronous subscribers");
    }

    private async Task NotifySubscribers(IEnumerable<Type> subscriberTypes, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var headSequenceNumber = await _eventStore.GetHeadSequenceNumber();
        var eventCache = new Dictionary<long, IEnumerable<EventEntry>>();

        foreach (var subscriberType in subscriberTypes)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var subscriber = (IEventSubscriber)scope.ServiceProvider.GetRequiredService(subscriberType);

            var currentState = await _subscriberStateStore.GetProcessingState(subscriber);

            if (!IsReadyForProcessing(currentState))
            {
                continue;
            }

            try
            {
                var events = await LoadEvents(subscriber, currentState, headSequenceNumber, eventCache);
                if (events.Any())
                {
                    await ApplyEvents(subscriber, currentState, events, cancellationToken);

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

    private bool IsReadyForProcessing(EventSubscriberProcessingState currentState)
    {
        if (currentState.ProcessingError == null)
        {
            return true;
        }

        // When processing fails we will exponentially backoff
        var backoffSeconds = currentState.ProcessingError.ProcessingAttempts * currentState.ProcessingError.ProcessingAttempts;
        if (currentState.ProcessingError.LatestRetryTime.AddSeconds(backoffSeconds) < DateTimeOffset.UtcNow)
        {
            return true;
        }

        return false;
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

    private async Task ApplyEvents(IEventSubscriber subscriber, EventSubscriberProcessingState currentState, IEnumerable<EventEntry> events, CancellationToken cancellationToken)
    {
        if (currentState.ProcessingError != null)
        {
            // Process one event at the time until we reach the failing one
            foreach (var @event in events)
            {
                await subscriber.Update([@event], cancellationToken);
            }
        }
        else
        {
            // We expect every thing to be ok, so lets try to process the entire batch
            await subscriber.Update(events, cancellationToken);
        }
    }

    protected IEnumerable<Type> GetSubscriberTypes()
    {
        return _subscriberRegister.AllSubscribers;
    }

    protected Task<EventSubscriberProcessingState> GetProcessingState(IEventSubscriber projector)
    {
        return _subscriberStateStore.GetProcessingState(projector);
    }
}
