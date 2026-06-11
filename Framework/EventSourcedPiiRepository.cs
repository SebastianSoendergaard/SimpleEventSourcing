using Basses.SimpleDocumentStore;
using Basses.SimpleEventStore.Enablers;
using Basses.SimpleEventStore.EventStore;

namespace UnderstandingEventsourcingExample.Framework;

public class EventSourcedPiiRepository<TAggregate, TPiiData>
    : EventSourcedRepository<TAggregate> where TAggregate : PiiAggregate<TPiiData>
{
    private readonly IDocumentStore _documentStore;

    public EventSourcedPiiRepository(IEventStore eventStore, IDocumentStore documentStore) : base(eventStore)
    {
        _documentStore = documentStore;
    }

    public new async Task Add(TAggregate aggregate)
    {
        // We have no transaction support so we save PII data first.
        // In case saving the events fails we will just use the version of PII that belongs to the last known event.
        // In case the first events of the aggregate fails we may leak PII data,
        // to prevent this do not add actual data on creation of the aggregate but add it later.
        await UpdatePiiStore(aggregate);
        await base.Add(aggregate);
    }

    public new async Task Update(TAggregate aggregate)
    {
        // We have no transaction support so we save PII data first.
        // In case saving the events fails we will just use the version of PII that belongs to the last known event.
        // In case the first events of the aggregate fails we may leak PII data,
        // to prevent this do not add actual data on creation of the aggregate but add it later.
        await UpdatePiiStore(aggregate);
        await base.Update(aggregate);
    }

    public async Task UpdatePiiStore(TAggregate aggregate)
    {
        if (aggregate.UncommitedPiiData == null)
        {
            return;
        }

        if (aggregate.UncommitedPiiData.Data == null)
        {
            await _documentStore.DeleteByIdAsync<PiiDataWrapper<TPiiData>>(aggregate.PiiDataId);
            return;
        }

        var piiDataVersions = await _documentStore.GetByIdAsync<List<PiiDataWrapper<TPiiData>>>(aggregate.PiiDataId);
        if (piiDataVersions == null)
        {
            piiDataVersions = new List<PiiDataWrapper<TPiiData>> { aggregate.UncommitedPiiData };
            await _documentStore.CreateAsync(piiDataVersions);
            return;
        }

        piiDataVersions.Add(aggregate.UncommitedPiiData);
        await _documentStore.UpdateAsync(aggregate.UncommitedPiiData);
    }

    public new async Task<TAggregate?> TryGet(string aggregateId)
    {
        var aggregate = await base.TryGet(aggregateId);

        if (aggregate != null)
        {
            // Restore PII data on aggregate if available
            var piiDataVersions = await _documentStore.GetByIdAsync<List<PiiDataWrapper<TPiiData>>>(aggregate.PiiDataId);
            var piiData = piiDataVersions?
                .OrderBy(x => x.Version)
                .LastOrDefault(x => x.Version <= aggregate.Version);

            if (piiData != null && piiData.Data != null)
            {
                aggregate.SetPiiData(piiData.Data);
            }
        }

        return aggregate;
    }
}
