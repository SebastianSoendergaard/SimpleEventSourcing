using Basses.SimpleEventStore.Enablers;

namespace Framework;

public abstract class PiiAggregate : Aggregate
{
    public IPiiData? UncommitedPiiData { get; protected set; }

    public void ClearUncommitedPiiData()
    {
        UncommitedPiiData = null;
    }
}
