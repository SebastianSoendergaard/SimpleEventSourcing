namespace UnderstandingEventsourcingExample.Framework;

public class PiiDataWrapper<T>
{
    public int Version { get; init; }
    public T? Data { get; init; }
}
