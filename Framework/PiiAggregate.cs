using Basses.SimpleEventStore.Enablers;

namespace UnderstandingEventsourcingExample.Framework;

public abstract class PiiAggregate<TPiiData> : Aggregate
{
    public PiiDataWrapper<TPiiData>? UncommitedPiiData { get; private set; }
    public Guid PiiDataId { get; private set; }
    protected TPiiData? PiiData { get; private set; }

    public PiiAggregate() { }

    public PiiAggregate(IEnumerable<IDomainEvent> events) : base(events) { }

    public void ClearUncommitedPiiData()
    {
        UncommitedPiiData = null;
    }

    public void SetPiiData(TPiiData data)
    {
        PiiData = data;
    }

    protected void SetPiiDataId(Guid id)
    {
        PiiDataId = id;
    }

    protected void UpdatePiiData(TPiiData? data)
    {
        UncommitedPiiData = new PiiDataWrapper<TPiiData>
        {
            Version = Version,
            Data = data
        };
        PiiData = data;
    }

    protected void ClearPiiData()
    {
        UncommitedPiiData = new PiiDataWrapper<TPiiData>
        {
            Version = Version
        };
        PiiData = default;
    }
}
